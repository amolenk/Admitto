interface PageLayoutProps {
    children: React.ReactNode;
}

export function PageLayout({ children }: PageLayoutProps) {
    return <div className="space-y-6">{children}</div>;
}
