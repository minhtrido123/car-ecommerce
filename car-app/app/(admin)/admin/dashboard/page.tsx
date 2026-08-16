export default function AdminDashboard() {
  return <>
    <div className="row">
      <div className="cell-md-6 mt-4">
        <div className="more-info-box bg-cyan fg-white">
          <div className="content">
            <h2 className="text-bold mb-0">150</h2>
            <div>New Orders</div>
          </div>
          <div className="icon">
            <span className="mif-cart"></span>
          </div>
          <a href="#" className="more"> More info <span className="mif-arrow-right"></span></a>
        </div>
      </div>
      <div className="cell-md-6 mt-4">
        <div className="more-info-box bg-green fg-white">
          <div className="content">
            <h2 className="text-bold mb-0">53%</h2>
            <div>Bounce Rate</div>
          </div>
          <div className="icon">
            <span className="mif-chart-bars"></span>
          </div>
          <a href="#" className="more"> More info <span className="mif-arrow-right"></span></a>
        </div>
      </div>
    </div>
    <div id="streamer"
      data-role="streamer"
      data-source="data/streamer_data.json"
      data-start-from="09:00"
      data-slide-to-start="false">
    </div>
  </>

}
