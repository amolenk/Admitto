import { readdirSync, readFileSync } from "node:fs";
import { join, resolve } from "node:path";
import ts from "typescript";
import { describe, expect, it } from "vitest";

const appRoot = resolve(__dirname, "..");

function findRouteFiles(directory: string): string[] {
    return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
        const path = join(directory, entry.name);
        if (entry.isDirectory()) {
            return findRouteFiles(path);
        }
        return entry.name === "route.ts" ? [path] : [];
    });
}

const httpMethods = new Set(["GET", "HEAD", "OPTIONS", "POST", "PUT", "PATCH", "DELETE"]);

function isExported(node: ts.Node): boolean {
    if (!ts.canHaveModifiers(node)) {
        return false;
    }
    return ts.getModifiers(node)?.some((modifier) => modifier.kind === ts.SyntaxKind.ExportKeyword) ?? false;
}

function isHttpMethod(name: ts.PropertyName | ts.BindingName | undefined): boolean {
    return !!name && ts.isIdentifier(name) && httpMethods.has(name.text);
}

function exportedHttpHandlers(sourceFile: ts.SourceFile): ts.Node[] {
    const handlers: ts.Node[] = [];

    for (const statement of sourceFile.statements) {
        if (ts.isFunctionDeclaration(statement) && isExported(statement) && isHttpMethod(statement.name)) {
            handlers.push(statement);
            continue;
        }

        if (!ts.isVariableStatement(statement) || !isExported(statement)) {
            continue;
        }

        for (const declaration of statement.declarationList.declarations) {
            if (!isHttpMethod(declaration.name) || !declaration.initializer) {
                continue;
            }
            if (ts.isArrowFunction(declaration.initializer) || ts.isFunctionExpression(declaration.initializer)) {
                handlers.push(declaration.initializer);
            }
        }
    }

    return handlers;
}

function hasWrapperInvocation(handler: ts.Node): boolean {
    let found = false;

    function visit(node: ts.Node): void {
        if (
            ts.isCallExpression(node) &&
            ts.isIdentifier(node.expression) &&
            node.expression.text === "callAdmittoApi"
        ) {
            found = true;
            return;
        }

        if (node !== handler && ts.isFunctionLike(node)) {
            return;
        }

        ts.forEachChild(node, visit);
    }

    visit(handler);
    return found;
}

describe("Admin API proxy conventions", () => {
    it("wraps every generated SDK HTTP handler with callAdmittoApi", () => {
        const violations: string[] = [];

        for (const routePath of findRouteFiles(appRoot)) {
            const source = readFileSync(routePath, "utf8");
            const sourceFile = ts.createSourceFile(routePath, source, ts.ScriptTarget.Latest, true, ts.ScriptKind.TS);
            const importsGeneratedSdk = sourceFile.statements.some(
                (statement) =>
                    ts.isImportDeclaration(statement) &&
                    ts.isStringLiteral(statement.moduleSpecifier) &&
                    /(?:^|\/)admitto-api\/generated(?:\/|$)/.test(statement.moduleSpecifier.text),
            );

            if (!importsGeneratedSdk) {
                continue;
            }

            for (const handler of exportedHttpHandlers(sourceFile)) {
                if (!hasWrapperInvocation(handler)) {
                    const name = ts.isFunctionDeclaration(handler)
                        ? handler.name?.text
                        : ts.isArrowFunction(handler) || ts.isFunctionExpression(handler)
                          ? "HTTP method"
                          : "HTTP method";
                    violations.push(`${routePath}:${name}`);
                }
            }
        }

        expect(violations).toEqual([]);
    });
});
