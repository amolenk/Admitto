"use client"

import {useHeader} from "@/components/header-context"
import {useEffect} from "react";

export default  function Home() {
    const {setTitle} = useHeader()

    useEffect(() => {
        setTitle("Admitto")
    }, [setTitle])

    return (
        <div>
        </div>
    );
}
