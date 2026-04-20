"use client"

import * as React from "react"

import { createContext, useContext } from 'react';

export type BreadcrumbItem = {
    label: string;
    href?: string;
};

type HeaderContextType = {
    title: string
    setTitle: (title: string) => void
    breadcrumbs: BreadcrumbItem[]
    setBreadcrumbs: (crumbs: BreadcrumbItem[]) => void
}

const HeaderContext = createContext<HeaderContextType | undefined>(undefined)

export const HeaderProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [title, setTitle] = React.useState("Admitto")
    const [breadcrumbs, setBreadcrumbs] = React.useState<BreadcrumbItem[]>([])

    return (
        <HeaderContext.Provider value={{ title, setTitle, breadcrumbs, setBreadcrumbs }}>
            {children}
        </HeaderContext.Provider>
    )
}

export const useHeader = () => {
    const context = useContext(HeaderContext)
    if (!context) {
        throw new Error("useHeader must be used within a HeaderProvider")
    }
    return context
}
