import type { Metadata } from "next";
import { SidebarInset, SidebarProvider } from "@/components/ui/sidebar";
import { AppSidebar } from "@/components/app-sidebar";
import { AppHeader } from "@/components/app-header";
import { Inter, Fraunces } from "next/font/google";
import { Geist_Mono } from "next/font/google";
import "@/globals.css";
import { HeaderProvider } from "@/components/header-context";
import { QueryProvider } from "@/components/query-provider";
import { Toaster } from "@/components/ui/sonner";
import { ThemeProvider } from "@/components/theme-provider";
import { redirect } from "next/navigation";
import { auth } from "@/lib/auth";
import { headers } from "next/headers";

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
    const session = await auth.api.getSession({
        headers: await headers()
    })

    if (!session) {
       redirect("/signin");
    }

    return (
        <html lang="en" suppressHydrationWarning>
        <body
            className={`${inter.variable} ${fraunces.variable} ${geistMono.variable} font-[family-name:var(--font-inter)] antialiased`}
        >
        <ThemeProvider attribute="class" defaultTheme="light" enableSystem disableTransitionOnChange>
        <QueryProvider>
            <HeaderProvider>
                <SidebarProvider>
                    <AppSidebar session={session} variant="inset" />
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
            <Toaster />
        </QueryProvider>
        </ThemeProvider>
        </body>
        </html>
    );
}
