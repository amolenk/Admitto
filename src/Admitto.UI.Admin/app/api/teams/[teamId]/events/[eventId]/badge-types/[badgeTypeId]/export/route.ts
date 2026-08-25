import { NextResponse } from "next/server";
import { callAdmittoApi } from "@/lib/admitto-api/admitto-client";
import { exportBadgeCsv } from "@/lib/admitto-api/generated";

export async function GET(
    _request: Request,
    { params }: { params: Promise<{ teamId: string; eventId: string; badgeTypeId: string }> }
) {
    const { teamId, eventId, badgeTypeId } = await params;

    return callAdmittoApi(
        () => exportBadgeCsv({ path: { teamId, eventId, badgeTypeId } }),
        {
            onSuccess: (result) => {
                const contentDisposition =
                    result.response.headers.get("content-disposition") ??
                    `attachment; filename="badges-${badgeTypeId}.csv"`;
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
