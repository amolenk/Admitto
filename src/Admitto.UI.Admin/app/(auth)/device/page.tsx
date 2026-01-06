export default async function DeviceConfirmationPage({ searchParams }: { searchParams?: { error?: string } }) {
    const error = (await searchParams)?.error;

    return (
        <div className="bg-muted flex min-h-svh flex-col items-center justify-center gap-6 p-6 md:p-10">
            <div className="flex w-full max-w-sm flex-col gap-6">
                {(error) && (
                    <>
                        <h1 className="text-lg font-semibold">Login failed</h1>
                        <p>{error}</p>
                    </>
                )}

                {!error && (
                    <>
                        <h1 className="text-lg font-semibold">You’re signed in</h1>
                        <p>Your login was successful. You can close this page.</p>
                    </>
                )}
            </div>
        </div>
    )
}
