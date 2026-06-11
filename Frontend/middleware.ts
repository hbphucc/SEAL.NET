import { NextRequest, NextResponse } from "next/server";
import { decodeJwt } from "jose";
import { getRequiredRoles, isPublicRoute } from "@/lib/constants";

function getTokenRoles(token: string): string[] {
  try {
    const payload = decodeJwt(token);
    const roleClaim =
      payload.role ??
      payload.roles ??
      payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

    if (Array.isArray(roleClaim)) return roleClaim.map(String);
    return roleClaim ? [String(roleClaim)] : [];
  } catch {
    return [];
  }
}

function isTokenExpired(token: string): boolean {
  try {
    const payload = decodeJwt(token);
    return typeof payload.exp === "number" && payload.exp * 1000 <= Date.now();
  } catch {
    return true;
  }
}

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (
    isPublicRoute(pathname) ||
    pathname.startsWith("/_next") ||
    pathname.startsWith("/favicon")
  ) {
    return NextResponse.next();
  }

  const token = request.cookies.get("seal_token")?.value;

  if (!token || isTokenExpired(token)) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

  const requiredRoles = getRequiredRoles(pathname);

  if (requiredRoles) {
    const tokenRoles = getTokenRoles(token);
    const hasRequiredRole = requiredRoles.some((role) =>
      tokenRoles.includes(role)
    );

    if (!hasRequiredRole) {
      return NextResponse.redirect(new URL("/unauthorized", request.url));
    }
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!api|_next/static|_next/image|favicon.ico).*)"],
};
