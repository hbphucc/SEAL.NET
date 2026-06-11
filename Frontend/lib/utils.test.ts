import { describe, it, expect } from "vitest";
import { getErrorMessage, formatDate, getTeamStatusColor } from "@/lib/utils";

describe("getErrorMessage", () => {
  it("prefers the API message field", () => {
    const err = { response: { data: { message: "Team name already exists." } } };
    expect(getErrorMessage(err)).toBe("Team name already exists.");
  });

  it("falls back to the first model-validation error", () => {
    const err = {
      response: { data: { errors: { Email: ["Email is required.", "second"] } } },
    };
    expect(getErrorMessage(err)).toBe("Email is required.");
  });

  it("falls back to the problem-details title", () => {
    const err = { response: { data: { title: "One or more validation errors occurred." } } };
    expect(getErrorMessage(err)).toBe("One or more validation errors occurred.");
  });

  it("uses the Error message for plain errors", () => {
    expect(getErrorMessage(new Error("boom"))).toBe("boom");
  });

  it("returns a default for unknown shapes", () => {
    expect(getErrorMessage("nope")).toBe("An unknown error occurred.");
  });
});

describe("formatDate", () => {
  it("returns a dash for empty input", () => {
    expect(formatDate(null)).toBe("—");
    expect(formatDate(undefined)).toBe("—");
  });

  it("formats a date string", () => {
    const formatted = formatDate("2026-06-11T10:00:00Z");
    expect(formatted).not.toBe("—");
    expect(formatted).toContain("2026");
  });
});

describe("getTeamStatusColor", () => {
  it("returns a class for a known status", () => {
    expect(getTeamStatusColor("Approved")).toContain("blue");
  });

  it("falls back to a neutral class for an unknown status", () => {
    // deliberately bypass the type to exercise the runtime fallback
    expect(getTeamStatusColor("Mystery" as never)).toContain("gray");
  });
});
