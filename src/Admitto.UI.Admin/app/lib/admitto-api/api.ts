// export async function callWithRefresh<T>(fn: () => Promise<T>): Promise<T | "UNAUTHORIZED"> {
//     let firstResult: any;
//
//     try {
//         firstResult = await fn();
//     } catch (err: any) {
//         // HeyAPI throws for non-2xx, including 401
//         if (err?.response?.status === 401) {
//             firstResult = err;
//         } else {
//             throw err; // Unknown error => let caller handle 500
//         }
//     }
//
//     // If the first attempt is not unauthorized → return it
//     const status =
//         firstResult?.response?.status ??
//         firstResult?.status ??
//         firstResult?.response?.statusCode ??
//         null;
//
//     if (status !== 401) {
//         return firstResult;
//     }
//
//     // Attempt token refresh
//     const refreshRes = await fetch(`${process.env.NEXT_PUBLIC_APP_URL}/api/auth/refresh`, {
//         method: "GET",
//         credentials: "include",
//     });
//
//     if (!refreshRes.ok) {
//         return "UNAUTHORIZED";
//     }
//
//     // Retry once with a fresh token
//     try {
//         return await fn();
//     } catch (err: any) {
//         if (err?.response?.status === 401) {
//             return "UNAUTHORIZED";
//         }
//         throw err;
//     }
// }