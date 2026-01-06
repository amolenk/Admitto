import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "@/globals.css";

const geistSans = Geist({
    variable: "--font-geist-sans",
    subsets: ["latin"]
});

const geistMono = Geist_Mono({
    variable: "--font-geist-mono",
    subsets: ["latin"]
});

export const metadata: Metadata = {
    title: "Admitto - Admin Dashboard",
    description: "Dashboard for managing events in Admitto"
};

export default async function RootLayout({
                                             children
                                         }: Readonly<{
    children: React.ReactNode;
}>)
{
    return (
        <html lang="en">
        <body
            className={`${geistSans.variable} ${geistMono.variable} font-[family-name:var(--font-geist-sans)] antialiased`}
        >
        <div className="flex flex-1 flex-col">
            <div className="@container/main flex flex-1 flex-col">
                {children}
            </div>
        </div>
        </body>
        </html>
    );
}
