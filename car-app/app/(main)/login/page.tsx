"use client";
import Link from "next/link";
import { LoginForm, loginSchema } from "../schema/login";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import api from "../lib/api";
import { useAuthStore } from "../stores/auth-store";
import { useRouter } from "next/navigation";
import { useEffect } from "react";

export default function Login() {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  });
  const router = useRouter();
  const setTokens = useAuthStore((state) => state.setTokens);
  const accessToken = useAuthStore((state) => state.accessToken);
  const setUser = useAuthStore((state) => state.setUser);
  const user = useAuthStore((state) => state.user);

  const onSubmit = async (input: LoginForm) => {
    try {
      const { data } = await api.post(`/auth/login`, input);
      setTokens(data.accessToken, data.refreshToken)
      setUser(data.user);
      if (data.user?.role === "Admin") {
        router.push("/admin/dashboard");
      } else {
        router.push("/");
      }
    }
    catch (err) {
      // console.log(err);
    }
  };
  useEffect(() => {
    if (accessToken) {
      router.replace(user?.role === "Admin" ? "/admin/dashboard" : "/");
    }
  }, [accessToken, user, router]);


  return (
    <form onSubmit={handleSubmit(onSubmit)} className="card p-10">
      <div className="form-group">
        <label>Email address</label>
        <input {...register("email")} type="text" data-role="input"
          data-prepend="<span class='mif-user'></span>" />
        <small className="text-muted">We&apos;ll never share your email with anyone else.</small>
        {errors.email && (
          <p className="text-red-500">{errors.email.message}</p>
        )}
      </div>
      <div className="form-group">
        <label>Password</label>
        <input {...register("password")} type="password" data-role="input" data-prepend="<span class='mif-lock'></span>" />
        {errors.password && (
          <p className="text-red-500">{errors.password.message}</p>
        )}
      </div>
      <div className="form-group">
        <input {...register("rememberMe")} type="checkbox" data-role="checkbox" data-caption="Remember me" />
      </div>
      <div>
        Don&apos;t have an account <Link href="/register">Register</Link>
      </div>
      <div className="form-group">
        <button type="submit" className="button success">Login</button>
        <input type="button" className="button" value="Cancel" />
      </div>
    </form>
  )
}
