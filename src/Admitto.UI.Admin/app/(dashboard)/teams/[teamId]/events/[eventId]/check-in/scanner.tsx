"use client";

import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, Camera, CameraOff, Check, RotateCcw, Search, Volume2, VolumeX } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import type { CheckInLookupCandidateDto, CheckInResponse, CheckInSummaryDto } from "@/lib/admitto-api/generated/types.gen";
import { apiClient } from "@/lib/api-client";
import { mapCheckInOutcome } from "@/lib/check-in";
import { formatInEventZone } from "@/lib/time-zones";

export type DecoderAdapter = {
    start: (onValue: (value: string) => void, facingMode: "environment" | "user") => Promise<void>;
    stop: () => Promise<void>;
};

export function createHtml5QrDecoder(elementId: string): DecoderAdapter {
    let scanner: any;
    return {
        async start(onValue, facingMode) {
            const { Html5Qrcode } = await import("html5-qrcode");
            scanner = new Html5Qrcode(elementId);
            await scanner.start({ facingMode }, { fps: 10, qrbox: { width: 260, height: 260 } }, onValue, () => undefined);
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
    summary?: CheckInSummaryDto;
};

type Status = {
    kind: "success" | "duplicate" | "cancelled" | "invalid" | "inactive" | "network";
    response?: CheckInResponse;
    message: string;
} | null;

export function CheckInScanner({ teamId, eventId, startsAt, timeZone, decoder: suppliedDecoder, summary }: Props) {
    const queryClient = useQueryClient();
    const defaultDecoder = useMemo(() => createHtml5QrDecoder("check-in-reader"), []);
    const decoder = suppliedDecoder ?? defaultDecoder;
    const [status, setStatus] = useState<Status>(null);
    const [searchQuery, setSearchQuery] = useState("");
    const [pendingCredential, setPendingCredential] = useState("");
    const [candidates, setCandidates] = useState<CheckInLookupCandidateDto[]>([]);
    const [selected, setSelected] = useState<CheckInLookupCandidateDto | null>(null);
    const [muted, setMuted] = useState(false);
    const [cameraOn, setCameraOn] = useState(true);
    const [facingMode, setFacingMode] = useState<"environment" | "user">("environment");
    const [warningAcknowledged, setWarningAcknowledged] = useState(false);
    const wedge = useRef("");
    const inFlight = useRef(false);
    const lastSequence = useRef("");
    const cameraPaused = useRef(false);
    const authoritativeCount = Number(summary?.checkedInCount ?? 0);

    const mutedRef = useRef(false);
    const sound = useCallback((success: boolean) => {
        if (mutedRef.current) return;
        try {
            const context = new AudioContext();
            const oscillator = context.createOscillator();
            oscillator.frequency.value = success ? 880 : 180;
            oscillator.connect(context.destination);
            oscillator.start();
            oscillator.stop(context.currentTime + 0.09);
        } catch { /* optional */ }
    }, []);

    const submit = useCallback(async (credential: string) => {
        const value = credential.trim();
        if (!value || inFlight.current || lastSequence.current === value) return;
        lastSequence.current = value;
        setPendingCredential(value);
        inFlight.current = true;
        try {
            const response = await apiClient.post<CheckInResponse>(
                `/api/teams/${teamId}/events/${eventId}/registrations/check-in`,
                { credential: value },
            );
            const outcome = mapCheckInOutcome(response);
            if (outcome.kind === "success") {
                const tickets = response.ticketSelections?.map((ticket) => ticket.name).join(", ");
                setStatus({ kind: "success", response, message: `${response.name ?? "Checked in"}${tickets ? ` · ${tickets}` : ""}` });
                setSelected(null);
                setPendingCredential("");
                cameraPaused.current = true;
                setCameraOn(false);
                void queryClient.invalidateQueries({ queryKey: ["check-in-summary", teamId, eventId] });
                sound(true);
            } else if (outcome.kind === "duplicate") {
                setStatus({ kind: "duplicate", response, message: `Already checked in${response.checkedInAt ? ` at ${formatInEventZone(response.checkedInAt, timeZone, "HH:mm")}` : ""}` });
                sound(false);
            } else if (outcome.kind === "cancelled") {
                setStatus({ kind: "cancelled", response, message: "Cancelled — Create Registration is required." });
                sound(false);
            } else if (outcome.kind === "inactive") {
                setStatus({ kind: "inactive", response, message: "This event is not active." });
                sound(false);
            } else {
                setStatus({ kind: "invalid", response, message: "This credential is not valid for this event." });
                sound(false);
            }
        } catch {
            lastSequence.current = "";
            setStatus({ kind: "network", message: "Network error. Your credential is retained — retry when ready." });
            sound(false);
        } finally {
            inFlight.current = false;
        }
    }, [eventId, queryClient, sound, teamId, timeZone]);

    const lookup = useCallback(async () => {
        if (searchQuery.trim().length < 2) return setCandidates([]);
        try {
            setCandidates(await apiClient.get<CheckInLookupCandidateDto[]>(`/api/teams/${teamId}/events/${eventId}/registrations/check-in/lookup?query=${encodeURIComponent(searchQuery)}`));
        } catch { /* retain pending values */ }
    }, [eventId, searchQuery, teamId]);

    useEffect(() => {
        let active = true;
        if (cameraOn) decoder.start((value) => active && !cameraPaused.current && void submit(value), facingMode).catch(() => undefined);
        return () => { active = false; void decoder.stop(); };
    }, [cameraOn, decoder, facingMode, submit]);

    useEffect(() => {
        const onKey = (event: KeyboardEvent) => {
            const target = event.target as HTMLElement | null;
            if (target?.isContentEditable || ["INPUT", "TEXTAREA", "SELECT"].includes(target?.tagName ?? "")) return;
            if (event.key === "Enter") { const value = wedge.current; wedge.current = ""; void submit(value); }
            else if (event.key.length === 1) wedge.current += event.key;
        };
        window.addEventListener("keydown", onKey);
        return () => window.removeEventListener("keydown", onKey);
    }, [submit]);

    useEffect(() => { const timer = window.setTimeout(() => void lookup(), 250); return () => window.clearTimeout(timer); }, [lookup]);

    const warning = Date.now() >= new Date(startsAt).getTime() - 30 * 60_000 && Date.now() < new Date(startsAt).getTime();
    const checkedIn = authoritativeCount;
    const expected = Number(summary?.expectedCount ?? 0);

    return (
        <div className="mx-auto w-full max-w-2xl space-y-5">
            {warning && !warningAcknowledged && <div className="flex items-center justify-between rounded-xl border border-amber-300/50 bg-amber-50 p-3 text-sm"><span><AlertTriangle className="mr-2 inline size-4" />Event starts at {formatInEventZone(startsAt, timeZone, "HH:mm")}</span><Button size="sm" variant="outline" onClick={() => setWarningAcknowledged(true)}>Got it</Button></div>}
            <Card className="overflow-hidden">
                <div className="flex items-center justify-between border-b p-4"><div><p className="text-xs uppercase tracking-widest text-muted-foreground">Live check-in</p><h1 className="font-display text-2xl font-semibold">Scan a ticket</h1><p className="text-sm text-muted-foreground">{summary ? `${checkedIn} checked in · ${expected} expected · ${expected ? Math.round(checkedIn / expected * 100) : 0}%` : "Check-in summary unavailable"}</p></div><div className="flex items-center gap-2"><Badge variant="secondary">Authoritative count</Badge><Button variant="ghost" size="icon" aria-label={muted ? "Unmute sound" : "Mute sound"} onClick={() => setMuted((value) => { mutedRef.current = !value; return !value; })}>{muted ? <VolumeX /> : <Volume2 />}</Button></div></div>
                <div className="bg-slate-950 p-4"><div id="check-in-reader" className="mx-auto aspect-square max-h-[55vh] w-full max-w-md rounded-2xl border border-white/20 bg-slate-900" /><div className="mt-3 flex justify-center gap-2">{!cameraPaused.current && <Button variant="secondary" onClick={() => setCameraOn((value) => !value)}>{cameraOn ? <CameraOff /> : <Camera />} {cameraOn ? "Stop camera" : "Start camera"}</Button>}<Button variant="secondary" onClick={() => setFacingMode((value) => value === "environment" ? "user" : "environment")}><Camera /> Switch camera</Button></div></div>
                <div className="space-y-3 p-4"><p className="text-center text-sm text-muted-foreground">Camera defaults to the rear camera. A connected QR scanner also works.</p><div className="flex gap-2"><Input aria-label="Manual search" placeholder="Search attendee by name or email" value={searchQuery} onChange={(event) => setSearchQuery(event.target.value)} /><Button variant="outline" aria-label="Search"><Search /></Button></div>{candidates.length > 0 && <div className="space-y-2">{candidates.map((candidate) => { const unavailable = candidate.state !== "eligible"; return <button type="button" disabled={unavailable} className="w-full rounded-lg border p-3 text-left hover:bg-muted disabled:cursor-not-allowed disabled:opacity-60" key={candidate.registrationId} onClick={() => setSelected(candidate)}><div className="font-medium">{candidate.name}</div><div className="text-xs text-muted-foreground">{candidate.email} · {candidate.state === "cancelled" ? "Cancelled" : candidate.state === "checkedIn" ? "Already checked in" : "Eligible"}</div></button>; })}</div>}</div>
            </Card>
            {status && <div role="alert" className="rounded-xl border p-4"><div className="flex items-center gap-2 font-medium">{status.kind === "success" && <Check className="size-4" />}{status.message}</div>{status.kind === "success" ? <Button className="mt-3" size="sm" onClick={() => { cameraPaused.current = false; lastSequence.current = ""; setStatus(null); setCameraOn(true); }}><Camera /> Scan next</Button> : <Button className="mt-3" size="sm" variant="outline" onClick={() => { setStatus(null); lastSequence.current = ""; if (status.kind === "network") void submit(pendingCredential); }}>{status.kind === "network" ? <><RotateCcw className="size-3.5" /> Retry</> : "Dismiss"}</Button>}</div>}
            {selected && <div role="dialog" aria-label={`Check in ${selected.name}?`} className="rounded-xl border bg-card p-4"><h2 className="font-semibold">Check in {selected.name}?</h2><p className="mt-1 text-sm text-muted-foreground">This marks the attendee as present.</p><div className="mt-3 flex gap-2"><Button onClick={() => void submit(selected.registrationId)}>Confirm check-in</Button><Button variant="outline" onClick={() => setSelected(null)}>Cancel</Button></div></div>}
        </div>
    );
}
