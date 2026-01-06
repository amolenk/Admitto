import { NextResponse } from "next/server";
import { callWithRefresh } from "./api";

// export async function apiProxy<T>(
//     fn: () => Promise<T>
// ): Promise<NextResponse> {
//     try {
//         const result = await callWithRefresh(fn);
//
//         if (result === "UNAUTHORIZED") {
//             return new NextResponse(null, { status: 401 });
//         }
//
//         const typed = result as any;
//
//         // Success path
//         if (typed?.response?.ok) {
//             return NextResponse.json(typed.data, {
//                 status: typed.response.status,
//             });
//         }
//
//         // Error returned in structured form by HeyAPI
//         if (typed?.response?.status) {
//             return NextResponse.json(
//                 typed?.error ?? { error: "Upstream API error" },
//                 { status: typed.response.status }
//             );
//         }
//
//         // Unexpected shape
//         return NextResponse.json(
//             { error: "Unexpected API response shape" },
//             { status: 500 }
//         );
//     } catch (err: any) {
//         // HeyAPI throws in weird cases or code bug
//         console.error("apiProxy unexpected error:", err);
//
//         if (err?.response?.status === 401) {
//             return new NextResponse(null, { status: 401 });
//         }
//
//         return NextResponse.json({ error: "Internal server error" }, { status: 500 });
//     }
// }