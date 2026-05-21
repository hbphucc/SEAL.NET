"use client";

import { useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useAuth } from "@/contexts/AuthContext";
import { getErrorMessage } from "@/lib/utils";
import { Eye, EyeOff, Trophy, UserPlus, CheckCircle } from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";

const registerSchema = z
  .object({
    fullName: z.string().min(2, "Full name must be at least 2 characters"),
    email: z.string().email("Invalid email address"),
    password: z.string().min(6, "Password must be at least 6 characters"),
    confirmPassword: z.string(),
    studentType: z.enum(["0", "1"]),
    studentCode: z.string().min(1, "Please enter your student ID"),
    schoolName: z.string().optional(),
  })
  .refine((d) => d.password === d.confirmPassword, {
    message: "Passwords do not match",
    path: ["confirmPassword"],
  })
  .refine(
    (d) => d.studentType !== "1" || (d.schoolName && d.schoolName.trim().length > 0),
    {
      message: "Please enter your university name",
      path: ["schoolName"],
    }
  );

type RegisterFormData = z.infer<typeof registerSchema>;

export default function RegisterPage() {
  const { register: authRegister } = useAuth();
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [isPending, setIsPending] = useState(false);
  const [success, setSuccess] = useState(false);

  const {
    register,
    control,
    handleSubmit,
    formState: { errors },
  } = useForm<RegisterFormData>({
    resolver: zodResolver(registerSchema),
    defaultValues: { studentType: "0" },
  });

  const studentType = useWatch({ control, name: "studentType" });

  async function onSubmit(data: RegisterFormData) {
    setError("");
    setIsPending(true);
    try {
      await authRegister({
        fullName: data.fullName,
        email: data.email,
        password: data.password,
        studentType: Number(data.studentType) as 0 | 1,
        studentCode: data.studentCode,
        schoolName: data.schoolName,
      });
      setSuccess(true);
      toast.success("Registration successful! Please wait for admin approval.");
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setIsPending(false);
    }
  }

  if (success) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-blue-900 flex items-center justify-center p-4">
        <div className="bg-white rounded-2xl shadow-2xl w-full max-w-md p-8 text-center">
          <div className="w-16 h-16 rounded-full bg-green-100 flex items-center justify-center mx-auto mb-4">
            <CheckCircle className="w-8 h-8 text-green-600" />
          </div>
          <h2 className="text-xl font-bold text-slate-900 mb-2">Registration Successful!</h2>
          <p className="text-slate-500 text-sm mb-6">
            Your account has been created and is pending admin approval. You will be able to log in once approved.
          </p>
          <Link
            href="/login"
            className="inline-flex items-center gap-2 px-6 py-2.5 bg-blue-600 text-white rounded-lg text-sm font-medium hover:bg-blue-700 transition-colors"
          >
            Back to Login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-slate-800 to-blue-900 flex items-center justify-center p-4">
      <div className="relative w-full max-w-md">
        <div className="bg-white rounded-2xl shadow-2xl overflow-hidden">
          {/* Header */}
          <div className="bg-gradient-to-r from-slate-900 to-blue-800 px-8 py-7 text-white">
            <div className="flex items-center gap-3 mb-3">
              <div className="w-9 h-9 rounded-xl bg-white/10 flex items-center justify-center">
                <Trophy className="w-4 h-4 text-white" />
              </div>
              <span className="text-lg font-bold tracking-tight">SEAL.NET</span>
            </div>
            <h1 className="text-2xl font-bold mb-1">Create an Account</h1>
            <p className="text-slate-300 text-sm">Register to participate</p>
          </div>

          {/* Form */}
          <div className="px-8 py-6 max-h-[70vh] overflow-y-auto">
            <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
              {error && (
                <div className="bg-red-50 border border-red-200 rounded-lg px-4 py-3 text-sm text-red-700">
                  {error}
                </div>
              )}

              {/* Full Name */}
              <FormField label="Full Name" error={errors.fullName?.message}>
                <input
                  {...register("fullName")}
                  type="text"
                  placeholder="John Doe"
                  className={inputClass}
                />
              </FormField>

              {/* Email */}
              <FormField label="Email" error={errors.email?.message}>
                <input
                  {...register("email")}
                  type="email"
                  placeholder="example@fpt.edu.vn"
                  className={inputClass}
                />
              </FormField>

              {/* Student Type */}
              <FormField label="Student Type" error={errors.studentType?.message}>
                <select {...register("studentType")} className={inputClass}>
                  <option value="0">FPT University</option>
                  <option value="1">Other University (External)</option>
                </select>
              </FormField>

              {/* School name - only for external */}
              {studentType === "1" && (
                <FormField label="University Name" error={errors.schoolName?.message}>
                  <input
                    {...register("schoolName")}
                    type="text"
                    placeholder="Your university name"
                    className={inputClass}
                  />
                </FormField>
              )}

              {/* Student Code */}
              <FormField label="Student ID" error={errors.studentCode?.message}>
                <input
                  {...register("studentCode")}
                  type="text"
                  placeholder="HE123456"
                  className={inputClass}
                />
              </FormField>

              {/* Password */}
              <FormField label="Password" error={errors.password?.message}>
                <div className="relative">
                  <input
                    {...register("password")}
                    type={showPassword ? "text" : "password"}
                    placeholder="••••••••"
                    className={`${inputClass} pr-11`}
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(!showPassword)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-slate-400"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </FormField>

              {/* Confirm Password */}
              <FormField label="Confirm Password" error={errors.confirmPassword?.message}>
                <input
                  {...register("confirmPassword")}
                  type={showPassword ? "text" : "password"}
                  placeholder="••••••••"
                  className={inputClass}
                />
              </FormField>

              <button
                type="submit"
                disabled={isPending}
                className="w-full py-2.5 bg-blue-600 text-white rounded-lg font-medium text-sm hover:bg-blue-700 transition-colors disabled:opacity-60 flex items-center justify-center gap-2 shadow-sm mt-2"
              >
                {isPending ? (
                  <>
                    <div className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                    Processing...
                  </>
                ) : (
                  <>
                    <UserPlus className="w-4 h-4" />
                    Register
                  </>
                )}
              </button>
            </form>

            <p className="mt-5 text-center text-sm text-slate-500">
              Already have an account?{" "}
              <Link href="/login" className="text-blue-600 hover:underline font-medium">
                Login
              </Link>
            </p>
          </div>
        </div>
      </div>
    </div>
  );
}

const inputClass =
  "w-full px-4 py-2.5 border border-slate-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent transition-all placeholder:text-slate-400 bg-white";

function FormField({
  label,
  error,
  children,
}: {
  label: string;
  error?: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <label className="block text-sm font-medium text-slate-700 mb-1.5">{label}</label>
      {children}
      {error && <p className="mt-1 text-xs text-red-500">{error}</p>}
    </div>
  );
}
