import { describe, it, expect } from "vitest";
import { isPublicRoute, getRequiredRoles } from "@/lib/constants";

describe("isPublicRoute", () => {
  it("treats listed routes and their subpaths as public", () => {
    expect(isPublicRoute("/")).toBe(true);
    expect(isPublicRoute("/login")).toBe(true);
    expect(isPublicRoute("/events")).toBe(true);
    expect(isPublicRoute("/events/123")).toBe(true);
    expect(isPublicRoute("/leaderboard")).toBe(true);
  });

  it("treats dashboard/protected routes as non-public", () => {
    expect(isPublicRoute("/dashboard")).toBe(false);
    expect(isPublicRoute("/admin")).toBe(false);
    expect(isPublicRoute("/my-team")).toBe(false);
  });

  it("does not treat a route that merely starts with a public name as public", () => {
    expect(isPublicRoute("/eventsomething")).toBe(false);
  });
});

describe("getRequiredRoles", () => {
  it("maps admin areas to the Admin role", () => {
    expect(getRequiredRoles("/admin/users")).toEqual(["Admin"]);
    expect(getRequiredRoles("/teams")).toEqual(["Admin"]);
    expect(getRequiredRoles("/eliminations")).toEqual(["Admin"]);
  });

  it("maps judge and participant areas", () => {
    expect(getRequiredRoles("/judge/submissions/1/score")).toEqual(["Judge"]);
    expect(getRequiredRoles("/submit")).toEqual(["Member", "TeamLeader"]);
    expect(getRequiredRoles("/my-team")).toEqual(["Member", "TeamLeader"]);
  });

  it("returns null for routes without a guard", () => {
    expect(getRequiredRoles("/dashboard")).toBeNull();
    expect(getRequiredRoles("/profile")).toBeNull();
  });
});
