import type { Metadata } from "next";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import { AppSidebar } from "@/components/app-sidebar";
import { AppHeader } from "@/components/app-header";
import { Geist, Geist_Mono } from "next/font/google";
import { auth } from "@/components/auth";
import "@/globals.css";
import { redirect } from "next/navigation";
import { HeaderProvider } from "@/components/header-context";

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

    const session = await auth();

    if (!session) redirect("/api/auth/signin");

    // TODO Must be sure that we've got a session

    return (
        <html lang="en">
        <body
            className={`${geistSans.variable} ${geistMono.variable} font-[family-name:var(--font-geist-sans)] antialiased`}
        >
        <HeaderProvider>
            <SidebarProvider>
                <AppSidebar variant="inset" session={session} />
                <SidebarInset>
                    <AppHeader />
                    <div className="flex flex-1 flex-col">
                        <div className="@container/main flex flex-1 flex-col gap-2 px-4 py-4 lg:px-6 lg:py-6">
                            {children}
                        </div>
                    </div>
                </SidebarInset>
            </SidebarProvider>
        </HeaderProvider>
        </body>
        </html>
    );
}
