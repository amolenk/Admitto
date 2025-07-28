"use client";

import { useEffect, useState } from "react";

export function useDataLoader<T>(loadDataFunction: () => Promise<T>)
{
    const [data, setData] = useState<T | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() =>
    {
        setLoading(true);
        setError(null);

        loadDataFunction()
            .then((result) => {
                setData(result);
            })
            .catch((err) => {
                setError("Failed to load data.");
                console.error(err);
            })
            .finally(() => {
                setLoading(false);
            });

        // async function loadData()
        // {
        //     setLoading(true);
        //     setError(null);
        //
        //     loadDataFunction()
        //         .then((result) => {
        //             setData(result);
        //         })
        //         .catch((err) => {
        //             setError("Failed to load data.");
        //             console.error(err);
        //         })
        //         .finally(() => {
        //             setLoading(false);
        //         });
        //     //
        //     // setLoading(true);
        //     // setError(null);
        //     //
        //     // try
        //     // {
        //     //     const result = await loadDataFunction();
        //     //     setData(result);
        //     // }
        //     // catch (err)
        //     // {
        //     //     setError("Failed to load data.");
        //     //     console.error(err);
        //     // }
        //     // finally
        //     // {
        //     //     setLoading(false);
        //     // }
        // }
        //
        // loadData();
    }, [loadDataFunction]);

    return { data, loading, error };
}