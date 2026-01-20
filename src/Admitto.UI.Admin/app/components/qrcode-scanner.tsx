'use client';

import {CheckInResponse} from "@/lib/admitto-api/generated";
import React, {useEffect, useRef, useState} from "react";

type ScanResult =
    | { kind: "idle" }
    | { kind: "scanning" }
    | { kind: "success"; firstName: string; lastName: string; status: string }
    | { kind: "failed"; error: string };

export function QrCodeScanner({
                                  teamSlug,
                                  eventSlug
                              }: {
    teamSlug: string,
    eventSlug: string
}) {
    const readerId = "qr-reader";
    const scannerRef = useRef<any>(null); // Html5QrcodeScanner instance
    const mountedRef = useRef(false);

    const [result, setResult] = useState<ScanResult>({kind: "idle"});
    const [isRenderingScanner, setIsRenderingScanner] = useState(false);

    async function pauseScanner() {
        try {
            const s = scannerRef.current;
            if (s?.pause) s.pause(true);
        } catch {
            // ignore
        }
    }

    async function clearScanner() {
        try {
            const s = scannerRef.current;
            if (s?.clear) await s.clear();
        } catch {
            // ignore
        }
    }

    async function resumeScanner() {
        setResult({ kind: "scanning" });

        // Let React hide the result UI / show the scanner container
        await new Promise((r) => setTimeout(r, 0));

        try {
            const s = scannerRef.current;
            if (s?.resume) {
                await s.resume();
                return;
            }
        } catch {
            // If resume fails, last resort fallback:
            // you can show an error message instructing a page refresh,
            // because reinitializing may trigger permission again on iOS.
            setResult({ kind: "failed", error: "Unable to resume camera. Please refresh the page." });
        }
    }

    async function callCheckIn(scanResult: string) {

        const parts = scanResult.split(":");
        if (parts.length < 2) {
            throw new Error("Invalid QR code format");
        }
        const attendeeId = parts[0];
        const signature = parts.slice(1).join(":");

        const endpoint = `/api/teams/${encodeURIComponent(teamSlug)}/events/${encodeURIComponent(
            eventSlug
        )}/public/${encodeURIComponent(attendeeId)}/checkin?signature=${encodeURIComponent(signature)}`;

        const res = await fetch(endpoint, {
            method: "POST",
            headers: {"Content-Type": "application/json"},
            cache: "no-store",
        });

        if (!res.ok) {
            const msg = await res.text().catch(() => "");
            throw new Error(msg || `Server returned ${res.status}`);
        }

        const data = (await res.json()) as CheckInResponse;

        console.log(data);

        return {
            firstName: data.firstName,
            lastName: data.lastName,
            status: data.attendeeStatus?.toString() ?? "Unknown",
        };
    }

    async function onScanSuccess(decodedText: string) {
        await pauseScanner();

        if (navigator.vibrate) navigator.vibrate(50);

        try {
            const info = await callCheckIn(decodedText);
            setResult({kind: "success", ...info});
        } catch (e: any) {
            setResult({kind: "failed", error: String(e?.message ?? e)});
        }
    }

    function onScanFailure(_e: any) {
        // ignore per-frame decode errors
    }

    async function renderScanner() {
        if (isRenderingScanner) return;
        setIsRenderingScanner(true);

        try {
            const mod = await import("html5-qrcode");
            const Html5QrcodeScanner = mod.Html5QrcodeScanner;

            // html5-qrcode manages its own DOM; ensure container is empty
            const host = document.getElementById(readerId);
            if (host) host.innerHTML = "";

            const scanner = new Html5QrcodeScanner(
                readerId,
                {fps: 10, qrbox: {width: 250, height: 250}},
                /* verbose= */ false
            );

            scannerRef.current = scanner;
            scanner.render(onScanSuccess, onScanFailure);
            setResult({kind: "scanning"});
        } finally {
            setIsRenderingScanner(false);
        }
    }

    useEffect(() => {
        if (mountedRef.current) return;
        mountedRef.current = true;

        renderScanner();

        return () => {
            clearScanner();
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const showInlineResult = result.kind === "success" || result.kind === "failed";

    return (
        <div className="min-h-[80vh] flex items-center justify-center px-4">
            <div className="w-full max-w-md rounded-xl border bg-white shadow-lg">
                <div className="p-6 text-center">
                    <h3 className="text-xl font-semibold mb-4">Ticket Scanner</h3>

                    <noscript>
                        <p className="text-sm text-gray-600">
                            JavaScript is required to use the camera scanner.
                        </p>
                    </noscript>

                    <div className={showInlineResult ? "hidden" : "mb-4 flex justify-center"}>
                        <div
                            id={readerId}
                            className="
                                w-[350px] max-w-full
                                [&_button]:appearance-auto
                                [&_button]:border [&_button]:rounded-md [&_button]:px-3 [&_button]:my-2
                                [&_button]:cursor-pointer
                                [&_select]:border [&_select]:rounded-md [&_select]:px-2 [&_select]:py-1
                              "
                        />
                    </div>

                    {showInlineResult && (
                        <div className="mt-3">
                            {result.kind === "failed" && (
                                <div>
                                    <h3 className="text-lg font-semibold text-red-600">
                                        Check-in Failed
                                    </h3>
                                    <p className="mt-2 text-sm text-gray-800">{result.error}</p>
                                </div>
                            )}

                            {result.kind === "success" && (
                                <div>
                                    <h3 className="text-lg font-semibold text-green-700 mb-3">
                                        Check-in Succeeded
                                    </h3>

                                    <div className="space-y-2 text-sm">
                                        <Row label="First Name" value={result.firstName}/>
                                        <Row label="Last Name" value={result.lastName}/>
                                        <Row label="Registration Status" value={result.status}/>
                                    </div>
                                </div>
                            )}

                            <div className="mt-4">
                                <button
                                    type="button"
                                    onClick={resumeScanner}
                                    className="inline-flex items-center justify-center rounded-md bg-blue-600 px-4 py-2 text-white hover:bg-blue-700"
                                >
                                    Scan Another
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
}

function Row({label, value}: { label: string; value: string }) {
    return (
        <div className="grid grid-cols-12 items-center gap-2">
            <div className="col-span-5 text-right font-semibold">{label}:</div>
            <div className="col-span-7 text-left break-words">{value}</div>
        </div>
    );
}