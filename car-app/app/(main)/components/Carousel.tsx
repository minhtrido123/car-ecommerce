import "../css/Carousel.css"

export default function Carousel() {
  return (
    <div className="bg-[url('/subtle-prism.svg')] dark:bg-[url('/subtle-prism-dark.svg')]">
      <div className="bg-transparent" data-role="carousel"
        data-bullets="false"
        // data-auto-start="true"
        data-cls-controls="fg-white"
        data-control-next="<button class='button bg-[var(--background)]'><span class='mif-chevron-right text-[var(--primary-foreground)]!'></span></button>"
        data-control-prev="<button class='button bg-[var(--background)]'><span class='mif-chevron-left text-[var(--primary-foreground)]!'></span></button>"
        data-period="10000"
        data-duration="500"
        data-height="500px">

        <div className="slide slide relative
        ">
          <div className="absolute left-[2%] top-[80%]">
            <div className="grid grid-cols-2 gap-2">
              <svg className="text-[var(--foreground)]" style={{ width: "100px", height: "100px" }} viewBox="0 0 480 480"><g fill="currentColor"><circle cx="120" cy="120" r="120"></circle><circle cx="120" cy="360" r="120"></circle><circle cx="360" cy="120" r="120"></circle><circle cx="360" cy="360" r="120"></circle></g></svg>
              <svg className="text-[var(--secondary)]" style={{ width: "100px", height: "100px" }} xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 480"><path d="M240 240A240 240 0 0 0 0 480h120a120 120 0 0 1 240 0h120a240 240 0 0 0-240-240ZM240 0A240 240 0 0 0 0 240h120a120 120 0 0 1 240 0h120A240 240 0 0 0 240 0Z" fill="currentColor"></path></svg>
            </div>
          </div>
          <div className="absolute left-[85%] top-[2%]">
            <div className="grid grid-cols-2 gap-2">
              <svg className="text-[var(--foreground)]" style={{ width: "100px", height: "100px" }} viewBox="0 0 480 480"><g fill="currentColor"><circle cx="120" cy="120" r="120"></circle><circle cx="120" cy="360" r="120"></circle><circle cx="360" cy="120" r="120"></circle><circle cx="360" cy="360" r="120"></circle></g></svg>
              <svg className="text-[var(--secondary)]" style={{ width: "100px", height: "100px" }} xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 480"><path d="M240 240A240 240 0 0 0 0 480h120a120 120 0 0 1 240 0h120a240 240 0 0 0-240-240ZM240 0A240 240 0 0 0 0 240h120a120 120 0 0 1 240 0h120A240 240 0 0 0 240 0Z" fill="currentColor"></path></svg>
              <svg className="text-[var(--secondary)]" style={{ width: "100px", height: "100px" }} xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 480"><path d="M480 240a160 160 0 0 0-94.1-145.9 160 160 0 0 0-291.8 0 160 160 0 0 0 0 291.8 160 160 0 0 0 291.8 0A160 160 0 0 0 480 240Zm-320 80V160h160v160H160Z" fill="currentColor"></path></svg>
              <svg className="text-[var(--foreground)]" style={{ width: "100px", height: "100px" }} xmlns="http://www.w3.org/2000/svg" viewBox="0 0 480 480"><path d="M480 240H240V0a240 240 0 0 1 240 240ZM240 480H0V240a240 240 0 0 1 240 240ZM480 480H240V240a240 240 0 0 1 240 240ZM240 240H0V0a240 240 0 0 1 240 240Z" fill="currentColor"></path></svg>
            </div>
          </div>
          <div className="row py-2 px-10 flex-align-center h-100 space-x-[5%]!">
            <div className="cell-md-4 text-center">
              <div className="absolute! -z-10! -left-[50]! h-[100%]! w-[100%]! rounded-[58%_42%_30%_70%/60%_30%_70%_40%] bg-[var(--secondary)]" />
              <img src="/supra.jpg" className="animate-[morph_10s_linear_infinite]!" />
            </div>
            <div className="cell-md-7 animate-[sliding_.5s_ease-out]">
              <h1 className="text-light">Find Your Dream Car</h1>
              <p className="mt-4 mb-4">Buy our cool, unique cars and enjoy the ride.</p>
              <button className="button large bg-[var(--secondary)] text-[var(--secondary-foreground)] transition-transform duration-100 hover:scale-110">Buy now...</button>
            </div>
          </div>
        </div>
        <div className="slide bg-contain! bg-center! flex justify-center">
          <div className="row px-20 py-2 flex-align-center h-100 space-x-[5%]!">
            <div className="cell-md-7 animate-[sliding_.5s_ease-out]">
              <h1 className="text-light">Cool car, low price</h1>
              <p className="mt-4 mb-4">Browse our collection of JDM, old school, normal cars.</p>
              <button className="button large bg-[var(--secondary)] text-[var(--secondary-foreground)] transition-transform duration-100 hover:scale-110">Buy now...</button>
            </div>
            <div className="cell-md-4 text-center">
              <img src="/r34.jpg" className="rounded-xl" />
            </div>
          </div>
        </div>
        <div className="slide bg-contain! bg-center! flex justify-center">
          <div className="row px-20 py-2 flex-align-center h-100 space-x-[5%]!">
            <div className="cell-md-7 animate-[sliding_.5s_ease-out]">
              <h1 className="text-light">We also sell car parts</h1>
              <p className="mt-4 mb-4">See our car parts and accessories collection.</p>
              <button className="button large bg-[var(--secondary)] text-[var(--secondary-foreground)] transition-transform duration-100 hover:scale-110">Buy now...</button>
            </div>
            <div className="cell-md-4 text-center">
              <img src="/evo.webp" className="rounded-xl" />
            </div>
          </div>
        </div>

      </div>
      <div
        className="wave w-full h-16 z-[3] bg-[var(--secondary)]
          animate-[wave_15s_linear_infinite]"
        style={{
          maskImage: "url('/wave.svg')",
          WebkitMaskImage: "url('/wave.svg')",

          maskSize: "200% 100%",
          WebkitMaskSize: "200% 100%",
        }}
      >
      </div>
    </div>
  )
};
