import "../css/Carousel.css"

export default function Carousel() {
  return (
    <div data-role="carousel"
      data-cls-bullet="bullet-big"
      data-auto-start="true"
      data-cls-controls="fg-white"
      data-bullets-position="right"
      data-control-next="<button class='button bg-black!'><span class='mif-chevron-right fg-white'></span></button>"
      data-control-prev="<button class='button bg-black!'><span class='mif-chevron-left fg-white'></span></button>"
      data-period="10000"
      data-duration="500"
      data-height="500px">

      <div className="slide p-2 pl-10 pr-10 bg-center! bg-norepeat! bg-cover! bg-[linear-gradient(to_right,#EF9801,#F8BC1A)]">
        <div className="row flex-align-center h-100 space-x-[5%]!">
          <div className="cell-md-4 text-center">
            <div className="absolute! -z-10! -left-[50]! h-[100%]! w-[100%]! rounded-[58%_42%_30%_70%/60%_30%_70%_40%] bg-blue-500" />
            <img src="/supra.jpg" className="animate-[morph_10s_linear_infinite]!" />
          </div>
          <div className="cell-md-7 animate-[sliding_.5s_ease-out]">

            <h1 className="text-light">Find Your Perfect Ride</h1>
            <p className="mt-4 mb-4">Every Journey Begins with the Right Car.</p>
            <button className="button large alert">Show more...</button>
          </div>
        </div>
      </div>
      <div className="slide bg-[linear-gradient(to_right,#EF9801,#F8BC1A)] bg-contain! bg-center! flex justify-center">
        <img src="/r34.jpg" />
      </div>
      <div className="slide bg-[linear-gradient(to_right,#EF9801,#F8BC1A)] bg-contain! bg-center! flex justify-center">
        <img src="/evo.webp" />
      </div>
    </div>
  )
};
