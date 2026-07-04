import type { MetadataRoute } from "next";

const appUrl = "https://www.admitto.org/";

export default function manifest(): MetadataRoute.Manifest {
    return {
        id: appUrl,
        name: "Admitto",
        short_name: "Admitto",
        description: "Dashboard for managing events in Admitto",
        start_url: appUrl,
        scope: appUrl,
        display: "standalone",
        background_color: "#ffffff",
        theme_color: "#ffffff",
        icons: [
            {
                src: "/apple-touch-icon.png",
                sizes: "180x180",
                type: "image/png",
                purpose: "any"
            },
            {
                src: "/favicon.svg?v=2",
                sizes: "any",
                type: "image/svg+xml",
                purpose: "any"
            }
        ]
    };
}
