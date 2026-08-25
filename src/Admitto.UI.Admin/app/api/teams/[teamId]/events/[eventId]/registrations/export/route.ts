import { NextResponse } from "next/server";
import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { exportRegistrationsCsv } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string }> }
) {
    const { teamId, eventId } = await params;

    return callAdmittoApi(
        () => exportRegistrationsCsv({ path: { teamId, eventId } }),
        {
            onSuccess: (result) => {
                const contentDisposition =
                    result.response.headers.get("content-disposition") ??
                    `attachment; filename="registrations-${eventId}.csv"`;
                return new NextResponse(result.data as string, {
                    status: result.response.status,
                    headers: {
                        "Content-Type": "text/csv; charset=utf-8",
                        "Content-Disposition": contentDisposition,
                    },
                });
            },
        },
    );
}
