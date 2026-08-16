import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
//import "metro4-dist/css/metro-all.min.css";

import "../globals.css";
import MetroProvider from "./components/MetroProvider";
import Navbar from "./components/Navbar";
import GlobalSpinner from "./components/Spinner";
import Script from "next/script";

// const geistSans = Geist({
//   variable: "--font-geist-sans",
//   subsets: ["latin"],
// });

// const geistMono = Geist_Mono({
//   variable: "--font-geist-mono",
//   subsets: ["latin"],
// });

export const metadata: Metadata = {
  title: "Supra",
  description: "Car website",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html
      lang="en"
      // className={`${geistSans.variable} ${geistMono.variable} h-full antialiased`}
      className={`h-full antialiased`}
    >
      <body className="min-h-full flex flex-col">
        <MetroProvider>
          <GlobalSpinner />
          <Navbar />
          {children}
        </MetroProvider>
      </body>

    </html>
  );
}
