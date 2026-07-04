import { NextResponse } from "next/server";
import { exportRegistrationsCsv } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;

    try {
        const result = await exportRegistrationsCsv({ path: { teamId, eventId } }) as any;

        if (result?.response?.ok) {
            const contentDisposition =
                result.response.headers.get("content-disposition") ??
                `attachment; filename="registrations-${eventId}.csv"`;
            return new NextResponse(result.data as string, {
                status: 200,
                headers: {
                    "Content-Type": "text/csv; charset=utf-8",
                    "Content-Disposition": contentDisposition,
                },
            });
        }

        return NextResponse.json(
            result?.error ?? { error: "Export failed" },
            { status: result?.response?.status ?? 500 }
        );
    } catch (err: any) {
        console.error("CSV export error:", err);
        if (err?.response?.status === 401) {
            return new NextResponse(null, { status: 401 });
        }
        return NextResponse.json({ error: "Internal server error" }, { status: 500 });
    }
}
