"use client";

export function Wordmark() {
    return (
        <div className="flex items-center gap-2.5 px-1.5 py-1">
            <div className="wordmark-ticket">
                <svg
                    width="14"
                    height="14"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2.2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                >
                    <path d="M4 8v8M8 6v12M12 6v12M16 6v12M20 8v8" />
                </svg>
            </div>
            <span className="font-display text-[17px] font-semibold tracking-tight">Admitto</span>
        </div>
    );
}
