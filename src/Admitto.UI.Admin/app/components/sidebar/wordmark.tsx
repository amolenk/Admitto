"use client";

export function Wordmark() {
    return (
        <div className="flex items-center gap-2.5 px-1.5 py-1">
            <img src="/favicon.svg?v=2" alt="" className="h-6 w-6" aria-hidden="true" />
            <span className="font-display text-[17px] font-semibold tracking-tight">Admitto</span>
        </div>
    );
}
