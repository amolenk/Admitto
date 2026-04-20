import type { Metadata } from "next";
import { Inter, Fraunces } from "next/font/google";
import { Geist_Mono } from "next/font/google";
import "@/globals.css";

const inter = Inter({
    variable: "--font-inter",
    subsets: ["latin"]
});

const fraunces = Fraunces({
    variable: "--font-fraunces",
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
            className={`${inter.variable} ${fraunces.variable} ${geistMono.variable} font-[family-name:var(--font-inter)] antialiased`}
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
