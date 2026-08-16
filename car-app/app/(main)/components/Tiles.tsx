import "../css/Tiles.css"

export default function Tiles() {

  return (
    <div className="example clear px-5!">
      <h1 className="p-5!">Start</h1>
      <div className="origin-left! tiles-grid tiles-group w-full! fg-white ml-0 mt-10 fg-dark">
        <div data-role="tile" className="bg-indigo fg-white tile-medium animate-[tile-enter_250ms_linear_forwards]">
          <img className="icon" src={"/engine-block.png"} />
          <span className="branding-bar">Engine Block</span>
          <span className="badge-bottom">30</span>
        </div>
        <div data-role="tile" className="bg-cyan fg-white tile-medium">
          <img className="icon" src={"/alternator.png"} />
          <span className="branding-bar">Alternator</span>
          <span className="badge-bottom">10</span>
        </div>
        <div data-role="tile" className="bg-orange fg-white tile-wide" data-size="wide">
          <img className="icon" src={"/battery.png"} />
          <span className="branding-bar">Battery</span>
        </div>

        <div data-role="tile" className="tile-small">
          <img className="icon" src={"/starter-motor.png"} />
          <span className="branding-bar">Engine Block</span>
          <span className="badge-bottom">30</span>
        </div>
        <div data-role="tile" className="bg-red fg-white tile-small" >
          <img className="icon" src={"/spark-plug.png"} />
          <span className="branding-bar">Engine Block</span>
          <span className="badge-bottom">30</span>
        </div>

        <div data-role="tile" className="bg-teal fg-white tile-small" >
          <img className="icon" src={"/radiator.png"} />
          <span className="branding-bar">Engine Block</span>
          <span className="badge-bottom">30</span>
        </div>
        <div data-role="tile" className="bg-brown fg-white tile-small" >
          <img className="icon" src={"/water-pump.png"} />
        </div>
        <div data-role="tile" className="bg-cyan fg-white tile-medium" >
          <img className="icon" src={"/oil-filter.png"} />
          <span className="branding-bar">Tables</span>
        </div>
        <div data-role="tile" className="bg-indigo fg-white tile-medium" >
          <img className="icon" src={"/air-filter.png"} />
          <span className="branding-bar">Github</span>
          <span className="badge-bottom">30</span>
        </div>
        <div data-role="tile" className="bg-cyan fg-white tile-medium">
          <img className="icon" src={"/fuel-pump.png"} />
          <span className="branding-bar">Email</span>
          <span className="badge-bottom">10</span>
        </div>

        <div data-role="tile" className="bg-cyan fg-white tile-medium" >
          <img className="icon" src={"/brake-disc.png"} />
          <span className="branding-bar">Tables</span>
        </div>

        <div data-role="tile" className="bg-cyan fg-white tile-medium" >
          <img className="icon" src={"/brake-pad.png"} />
          <span className="branding-bar">Tables</span>
        </div>

        <div data-role="tile" className="tile-small">
          <img className="icon" src={"/shock-absorber.png"} />
        </div>
        <div data-role="tile" className="bg-red fg-white tile-small" >
          <img className="icon" src={"/strut.png"} />
        </div>

        <div data-role="tile" className="bg-teal fg-white tile-small" >
          <img className="icon" src={"/clutch-kit.png"} />
        </div>
        <div data-role="tile" className="bg-brown fg-white tile-small" >
          <img className="icon" src={"/turbo-charger.png"} />
        </div>
        <div data-role="tile" className="bg-cyan fg-white tile-medium" >
          <img className="icon" src={"/muffler.png"} />
          <span className="branding-bar">Tables</span>
        </div>
        <div data-role="tile" className="bg-indigo fg-white tile-medium" >
          <img className="icon" src={"/axle.png"} />
          <span className="branding-bar">Github</span>
          <span className="badge-bottom">30</span>
        </div>
        <div data-role="tile" className="bg-orange fg-white tile-wide" data-size="wide">
          <img className="icon" src={"/head-light.png"} />
          <span className="branding-bar">Chrome</span>
        </div>
        <div data-role="tile" className="bg-orange fg-white tile-wide" data-size="wide">
          <img className="icon" src={"/tail-light.png"} />
          <span className="branding-bar">Chrome</span>
        </div>
      </div>
    </div>
  );
}
