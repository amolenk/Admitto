"use client";

import CodeMirror from "@uiw/react-codemirror";
import { html } from "@codemirror/lang-html";
import { cn } from "@/lib/utils";

const HTML_EXTENSIONS = [html()];

interface CodeEditorProps {
    value: string;
    onChange: (value: string) => void;
    minHeight?: string;
    className?: string;
}

export function CodeEditor({ value, onChange, minHeight = "200px", className }: CodeEditorProps) {
    return (
        <div
            className={cn(
                "rounded-md border border-input overflow-hidden text-sm shadow-xs",
                "focus-within:border-ring focus-within:ring-[3px] focus-within:ring-ring/50",
                className
            )}
        >
            <CodeMirror
                value={value}
                onChange={onChange}
                extensions={HTML_EXTENSIONS}
                minHeight={minHeight}
                basicSetup={{
                    lineNumbers: true,
                    foldGutter: false,
                    dropCursor: false,
                    allowMultipleSelections: false,
                    indentOnInput: true,
                    bracketMatching: true,
                    closeBrackets: true,
                    autocompletion: false,
                    highlightSelectionMatches: false,
                }}
                style={{ fontSize: "12px" }}
            />
        </div>
    );
}
