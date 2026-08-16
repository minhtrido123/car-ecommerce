// app/(admin)/layout.tsx
import { Metadata } from "next";
import MetroProvider from "../(main)/components/MetroProvider";
import GlobalSpinner from "../(main)/components/Spinner";
import "../globals.css";
import Link from "next/link";
import SideNav from "./components/SideNav";
import RequireAdmin from "./components/RequireAdmin";

export const metadata: Metadata = {
  title: "Supra",
  description: "Car website",
};

export default function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html
      lang="en"
      // className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
      className={`h-full antialiased`}
    >
      <body>
        <MetroProvider>
          <GlobalSpinner />
          <RequireAdmin>
            <div className="min-h-screen flex">
              <SideNav />
              <div className="w-full p-5">

                {children}

              </div>



            </div>
          </RequireAdmin>
        </MetroProvider>
      </body>
    </html>

  );
}
