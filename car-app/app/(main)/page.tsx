import Carousel from "./components/Carousel";
import "./css/Home.css"

export default function Home() {
  return (
    <div className="px-0!">
      <Carousel />
      <div className="bg-[var(--secondary)]! bg-center! bg-cover! bg-norepeat! text-white! py-10!" >
        <h2 className="text-3xl font-semibold text-center">Our Sales</h2>
        <div className="flex! justify-center!">
          <div style={{ display: "grid" }} className="grid-cols-1! md:grid-cols-1! lg:grid-cols-3! gap-6! text-center! w-full! px-[5%]!">
            <div className="p-10! border! border-dashed! rounded-md!">
              <div className="flex! justify-center! items-center!">
                <div className="h1" data-role="counter"
                  data-value="20" id="counter-1">0</div>
                <div className="h1 mb-[1rem]! mt-0! ml-1!">+</div>
              </div>
              <div>Vehicle Categories</div>
            </div>
            <div className="p-10! border! border-dashed! rounded-md!">
              <div className="flex! justify-center! items-center!">
                <div className="h1" data-role="counter"
                  data-value="5000" id="counter-2">0</div>
                <div className="h1 mb-[1rem]! mt-0! ml-1!">+</div>
              </div>
              <div>Quality Auto Parts</div>
            </div>
            <div className="p-10! border! border-dashed! rounded-md!">
              <div className="flex! justify-center! items-center!">
                <div className="h1" data-role="counter"
                  data-value="100" id="counter-3">0</div>
                <div className="h1 mb-[1rem]! mt-0! ml-1!">+</div>
              </div>
              <div>Trusted Brands</div>
            </div>
          </div>
        </div>
      </div>


      <div className="relative w-full h-16">
        <div
          className="wave w-full h-16 z-[3] bg-[var(--secondary)] rotate-180 animate-[wave_14s_linear_infinite]"
          style={{
            maskImage: "url('/wave.svg')",
            WebkitMaskImage: "url('/wave.svg')",

            maskSize: "200% 100%",
            WebkitMaskSize: "200% 100%",
          }}
        >
        </div>
      </div>
      <div className="marquee-section py-10!">
        <h2 className="h1 text-center">Our Brands</h2>
        <div className="flex! justify-center!">
          <div style={{ display: "grid" }} className="grid-cols-2! lg:grid-cols-3! gap-12!">
            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/ford.png"
                className="mx-auto! grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>

            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/hyundai.png"
                className="h-full!  grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>

            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/toyota.webp"
                className="h-full! grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>

            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/aston-martin.png"
                className="mx-auto! grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>

            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/honda.webp"
                className="h-full!  grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>

            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/mercedes2.png"
                className="h-full! grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>

            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/bmw.png"
                className="h-full! mx-auto! grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>

            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/vw.png"
                className="h-full!  grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>

            <div className="flex! justify-center! items-center! w-[200px]! h-[100px]!">
              <img
                src="/nissan.png"
                className="h-full! grayscale hover:grayscale-0 transition duration-300"
                alt="Logo"
              />
            </div>
          </div>
        </div>

      </div>

      <div className="footer text-white! text-center! py-10! relative! bg-[url('/DesktopBackground/19_classicsportscars_astonmartindb4.jpg')]! bg-cover! bg-center!">
        <div className="absolute! inset-0! bg-black/50!"></div>

        <div className="relative z-10">
          <div>
            License Author Issues Releases Npm Nuget
          </div>
          <div className="flex! justify-center! space-x-2! py-5!">
            <div className="text-6xl! text-green-500!">Made by</div>
            <div className="text-6xl!">Supra</div>
          </div>
          <p>Metro UI (Metro UI CSS) © 2012-2020 by Serhii Pimenov.</p>
          <p>Domain by Imena.ua. Hosting by Mirohost.</p>
          <p>Metro CDN by KeyCDN.</p>
          <p>IDE PhpStorm by JetBrains.</p>
          <p>Docs version 2020.1. Code licensed MIT, docs CC BY 3.0. </p>
        </div>
      </div>


    </div >
  );
}
