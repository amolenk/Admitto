"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Camera, CameraOff, Check, RotateCcw, Search, Volume2, VolumeX } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import type { CheckInLookupCandidateDto, CheckInResponse } from "@/lib/admitto-api/generated/types.gen";
import { mapCheckInOutcome } from "@/lib/check-in";
import { formatInEventZone } from "@/lib/time-zones";
import { createDashboardCheckInOperations, type CheckInOperations } from "./check-in-operations";

export type DecoderAdapter = {
    start: (onValue: (value: string) => void, facingMode: "environment" | "user") => Promise<void>;
    stop: () => Promise<void>;
};

export function createHtml5QrDecoder(elementId: string): DecoderAdapter {
    let scanner: { stop: () => Promise<void>; clear: () => void } | undefined;
    return {
        async start(onValue, facingMode) {
            const { Html5Qrcode } = await import("html5-qrcode");
            const current = new Html5Qrcode(elementId);
            scanner = current;
            await current.start({ facingMode }, { fps: 10, qrbox: { width: 260, height: 260 } }, onValue, () => undefined);
        },
        async stop() {
            const current = scanner;
            scanner = undefined;
            await current?.stop().catch(() => undefined);
            current?.clear();
        },
    };
}

type Props = {
    teamId: string;
    eventId: string;
    startsAt: string;
    timeZone: string;
    decoder?: DecoderAdapter;
    operations?: CheckInOperations;
};

type Status = {
    kind: "success" | "duplicate" | "cancelled" | "invalid" | "inactive" | "network";
    response?: CheckInResponse;
    message: string;
} | null;

export function CheckInScanner({ teamId, eventId, startsAt, timeZone, decoder: suppliedDecoder, operations: suppliedOperations }: Props) {
    const queryClient = useQueryClient();
    const defaultDecoder = useMemo(() => createHtml5QrDecoder("check-in-reader"), []);
    const decoder = suppliedDecoder ?? defaultDecoder;
    const defaultOperations = useMemo(() => createDashboardCheckInOperations(teamId, eventId, queryClient), [teamId, eventId, queryClient]);
    const operations = suppliedOperations ?? defaultOperations;
    const [status, setStatus] = useState<Status>(null);
    const [searchQuery, setSearchQuery] = useState("");
    const [pendingCredential, setPendingCredential] = useState("");
    const [candidates, setCandidates] = useState<CheckInLookupCandidateDto[]>([]);
    const [selected, setSelected] = useState<CheckInLookupCandidateDto | null>(null);
    const [muted, setMuted] = useState(false);
    const [cameraOn, setCameraOn] = useState(true);
    const [facingMode, setFacingMode] = useState<"environment" | "user">("environment");
    const [warningAcknowledged, setWarningAcknowledged] = useState(false);
    const [now, setNow] = useState(() => Date.now());
    const wedge = useRef("");
    const inFlight = useRef(false);
    const lastSequence = useRef("");
    const cameraPaused = useRef(false);
    const successTimer = useRef<number | undefined>(undefined);
    const statusRef = useRef<Status>(null);
    const cameraOperation = useRef(Promise.resolve());
    const audioContext = useRef<AudioContext | null>(null);

    const mutedRef = useRef(false);
    const setScannerStatus = useCallback((next: Status) => {
        statusRef.current = next;
        setStatus(next);
    }, []);
    const getAudioContext = useCallback(() => {
        try {
            audioContext.current ??= new AudioContext();
            if (audioContext.current.state === "suspended") void audioContext.current.resume();
            return audioContext.current;
        } catch { /* optional */ }
        return null;
    }, []);
    const initializeAudio = useCallback(() => { void getAudioContext(); }, [getAudioContext]);
    const sound = useCallback((success: boolean) => {
        if (mutedRef.current) return;
        try {
            const context = getAudioContext();
            if (!context) return;
            const oscillator = context.createOscillator();
            oscillator.frequency.value = success ? 880 : 180;
            oscillator.connect(context.destination);
            oscillator.start();
            oscillator.stop(context.currentTime + 0.09);
        } catch { /* optional */ }
    }, [getAudioContext]);

    const outcomeStatus = useCallback((response: CheckInResponse, kind: Exclude<Status, null>["kind"]): Status => {
        if (kind === "duplicate") return { kind, response, message: `Already checked in${response.checkedInAt ? ` at ${formatInEventZone(response.checkedInAt, timeZone, "HH:mm")}` : ""}` };
        if (kind === "cancelled") return { kind, response, message: operations.createRegistrationHref ? "Cancelled — Create Registration is required." : "Cancelled." };
        if (kind === "inactive") return { kind, response, message: "This event is not active." };
        return { kind: "invalid", response, message: "This credential is not valid for this event." };
    }, [operations.createRegistrationHref, timeZone]);

    const submit = useCallback(async (credential: string) => {
        const value = credential.trim();
        if (!value || inFlight.current || statusRef.current || lastSequence.current === value) return;
        lastSequence.current = value;
        setPendingCredential(value);
        inFlight.current = true;
        try {
            const response = await operations.checkIn(value);
            const outcome = mapCheckInOutcome(response);
            if (outcome.kind === "success") {
                const tickets = response.ticketSelections?.map((ticket) => ticket.name).join(", ");
                setScannerStatus({ kind: "success", response, message: `${response.name ?? "Checked in"}${tickets ? ` · ${tickets}` : ""}` });
                setSelected(null);
                setPendingCredential("");
                cameraPaused.current = true;
                setCameraOn(false);
                operations.onCheckedIn?.(response);
                sound(true);
                successTimer.current = window.setTimeout(() => {
                    lastSequence.current = "";
                    cameraPaused.current = false;
                    setScannerStatus(null);
                    setCameraOn(true);
                }, 2000);
            } else {
                setScannerStatus(outcomeStatus(response, outcome.kind));
                sound(false);
            }
        } catch {
            setScannerStatus({ kind: "network", message: "Network error. Your credential is retained — retry when ready." });
            sound(false);
        } finally {
            inFlight.current = false;
        }
    }, [operations, outcomeStatus, setScannerStatus, sound]);

    const lookupEnabled = operations.supportsLookup ?? true;

    const lookup = useCallback(async () => {
        if (searchQuery.trim().length < 2) return setCandidates([]);
        try {
            setCandidates(await operations.lookup(searchQuery));
        } catch { /* retain pending values */ }
    }, [operations, searchQuery]);

    useEffect(() => () => { if (successTimer.current) window.clearTimeout(successTimer.current); }, []);
    useEffect(() => {
        const timer = window.setInterval(() => setNow(Date.now()), 1000);
        return () => window.clearInterval(timer);
    }, []);

    useEffect(() => {
        let active = true;
        cameraOperation.current = cameraOperation.current
            .then(async () => {
                await decoder.stop();
                if (active && cameraOn) await decoder.start((value) => active && !cameraPaused.current && void submit(value), facingMode);
            })
            .catch(() => undefined);
        return () => {
            active = false;
            cameraOperation.current = cameraOperation.current.then(() => decoder.stop()).catch(() => undefined);
        };
    }, [cameraOn, decoder, facingMode, submit]);

    useEffect(() => {
        const onKey = (event: KeyboardEvent) => {
            const target = event.target as HTMLElement | null;
            if (target?.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(target?.tagName ?? "")) return;
            if (statusRef.current) { wedge.current = ""; return; }
            if (event.key === "Enter") { const value = wedge.current; wedge.current = ""; void submit(value); }
            else if (event.key.length === 1) wedge.current += event.key;
        };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [submit]);

    useEffect(() => { if (!lookupEnabled) return; const timer = window.setTimeout(() => void lookup(), 250); return () => window.clearTimeout(timer); }, [lookup, lookupEnabled]);

    const warning = now >= new Date(startsAt).getTime() - 30 * 60_000 && now < new Date(startsAt).getTime();
    return (
        <div className="mx-auto w-full max-w-2xl space-y-5" onPointerDown={initializeAudio}>
            {warning && !warningAcknowledged && <div className="flex items-center justify-between rounded-xl border border-amber-300/50 bg-amber-50 p-3 text-sm"><span><AlertTriangle className="mr-2 inline size-4" />Event starts at {formatInEventZone(startsAt, timeZone, "HH:mm")}</span><Button size="sm" variant="outline" onClick={() => setWarningAcknowledged(true)}>Got it</Button></div>}
            <Card className="overflow-hidden">
                <div className="flex items-center justify-between border-b p-4"><div><p className="text-xs uppercase tracking-widest text-muted-foreground">Live check-in</p><h1 className="font-display text-2xl font-semibold">Scan a ticket</h1></div><Button variant="ghost" size="icon" aria-label={muted ? "Unmute sound" : "Mute sound"} onClick={() => setMuted((value) => { mutedRef.current = !value; return !value; })}>{muted ? <VolumeX /> : <Volume2 />}</Button></div>
                 <div className="overflow-hidden bg-slate-950 p-4"><div id="check-in-reader" data-testid="check-in-reader" style={{ contain: "layout paint" }} className="mx-auto aspect-square max-h-[55vh] min-w-0 w-full max-w-md overflow-hidden rounded-2xl border border-white/20 bg-slate-900 [&_img]:block [&_img]:h-full [&_img]:max-w-full [&_img]:!w-full [&_img]:object-cover [&_video]:block [&_video]:h-full [&_video]:max-w-full [&_video]:!w-full [&_video]:object-cover" /><div className="mt-3 flex flex-col gap-2 sm:flex-row sm:flex-wrap sm:justify-center">{!cameraPaused.current && <Button className="w-full sm:w-auto" variant="secondary" onClick={() => setCameraOn((value) => !value)}>{cameraOn ? <CameraOff /> : <Camera />} {cameraOn ? "Stop camera" : "Start camera"}</Button>}<Button className="w-full sm:w-auto" variant="secondary" onClick={() => setFacingMode((value) => value === "environment" ? "user" : "environment")}><Camera /> Switch camera</Button></div></div>
                <div className="space-y-3 p-4"><p className="text-center text-sm text-muted-foreground">Camera defaults to the rear camera. A connected QR scanner also works.</p>{lookupEnabled && <><div className="flex gap-2"><Input aria-label="Manual search" placeholder="Search attendee by name or email" value={searchQuery} onChange={(event) => setSearchQuery(event.target.value)} /><Button variant="outline" aria-label="Search"><Search /></Button></div>{candidates.length > 0 && <div className="space-y-2">{candidates.map((candidate) => { const unavailable = candidate.state !== "eligible"; return <button type="button" disabled={unavailable} className="w-full rounded-lg border p-3 text-left hover:bg-muted disabled:cursor-not-allowed disabled:opacity-60" key={candidate.registrationId} onClick={() => setSelected(candidate)}><div className="font-medium">{candidate.name}</div><div className="text-xs text-muted-foreground">{candidate.email} · {candidate.state === "cancelled" ? "Cancelled" : candidate.state === "checkedIn" ? "Already checked in" : "Eligible"}</div></button>; })}</div>}</>}</div>
            </Card>
            {status && <div role="alert" className={`rounded-xl border p-4 ${status.kind === "success" ? "border-emerald-300 bg-emerald-50 text-emerald-950" : "border-amber-300/60 bg-amber-50"}`}><div className="flex items-center gap-2 font-medium">{status.kind === "success" && <Check className="size-4" />}{status.message}</div>{status.kind === "success" ? <p className="mt-1 text-xs opacity-70">Ready for the next scan</p> : <div className="mt-3 flex flex-wrap gap-2">{status.kind === "cancelled" && operations.createRegistrationHref && <Button asChild size="sm"><Link href={operations.createRegistrationHref}>Create registration</Link></Button>}{status.kind === "network" && <Button size="sm" variant="outline" onClick={() => { setScannerStatus(null); lastSequence.current = ""; void submit(pendingCredential); }}><RotateCcw className="size-3.5" /> Retry</Button>}<Button size="sm" variant="outline" onClick={() => { setScannerStatus(null); lastSequence.current = ""; }}>Dismiss</Button></div>}</div>}
            {selected && <div role="dialog" aria-label={`Check in ${selected.name}?`} className="rounded-xl border bg-card p-4"><h2 className="font-semibold">Check in {selected.name}?</h2><p className="mt-1 text-sm text-muted-foreground">This marks the attendee as present.</p><div className="mt-3 flex gap-2"><Button onClick={() => void submit(selected.registrationId)}>Confirm check-in</Button><Button variant="outline" onClick={() => setSelected(null)}>Cancel</Button></div></div>}
        </div>
    );
}
