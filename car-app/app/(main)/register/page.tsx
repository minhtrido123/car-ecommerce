"use client";
import Link from "next/link";
import { LoginForm, loginSchema } from "../schema/login";
import { zodResolver } from "@hookform/resolvers/zod";
import { useForm } from "react-hook-form";
import api from "../lib/api";
import { useAuthStore } from "../stores/auth-store";
import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { RegisterForm, registerSchema } from "../schema/register";

export default function Register() {
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterForm>({
    resolver: zodResolver(registerSchema),
  });
  const router = useRouter();
  const setTokens = useAuthStore((state) => state.setTokens);
  const accessToken = useAuthStore((state) => state.accessToken);
  const setUser = useAuthStore((state) => state.setUser);

  const onSubmit = async (input: LoginForm) => {
    try {
      const { data } = await api.post(`/auth/register`, input);
      setTokens(data.accessToken, data.refreshToken)
      setUser(data.user);
      router.push("/")
      // console.log(data)
    }
    catch (err) {
      // console.log(err);
    }
  };
  useEffect(() => {
    if (accessToken) {
      router.replace("/");
    }
  }, [accessToken, router]);


  return (
    <form onSubmit={handleSubmit(onSubmit)} className="card p-10">
      <div className="form-group">
        <label>User name</label>
        <input {...register("name")} type="text" data-role="input"
          data-prepend="<span class='mif-user'></span>" />
        {errors.name && (
          <p className="text-red-500">{errors.name.message}</p>
        )}
      </div>
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
        Don&apos;t have an account <Link href="/login">Login</Link>
      </div>
      <div className="form-group">
        <button type="submit" className="button success">Register</button>
        <input type="button" className="button" value="Cancel" />
      </div>
    </form>
  )
}
