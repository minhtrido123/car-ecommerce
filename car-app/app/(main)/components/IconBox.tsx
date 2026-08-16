export default function IconBox() {
  return (
    <div className="example">
      <div className="row mt-2">
        <div className="cell-md-4 mt-4">
          <div className="icon-box border bd-default">
            <div className="icon bg-cyan fg-white"><span className="mif-cog"></span></div>
            <div className="content p-4">
              <div className="text-upper">cpu traffic</div>
              <div className="text-upper text-bold text-lead">90%</div>
            </div>
          </div>
        </div>
        <div className="cell-md-4 mt-4">
          <div className="icon-box border bd-default">
            <div className="icon bg-red fg-white"><span className="mif-google-plus"></span></div>
            <div className="content p-4">
              <div className="text-upper">likes</div>
              <div className="text-upper text-bold text-lead">41,410</div>
            </div>
          </div>
        </div>
        <div className="cell-md-4 mt-4">
          <div className="icon-box border bd-default">
            <div className="icon bg-green fg-white"><span className="mif-cart"></span></div>
            <div className="content p-4">
              <div className="text-upper">sales</div>
              <div className="text-upper text-bold text-lead">1024</div>
            </div>
          </div>
        </div>
      </div>

      <div className="row">
        <div className="cell-md-4 mt-4">
          <div className="icon-box bg-cyan fg-white">
            <div className="icon"><span className="mif-bookmark"></span></div>
            <div className="content">
              <div className="p-2">
                <div>BOOKMARKS</div>
                <div className="text-bold text-leader">41,400</div>
              </div>
              <div data-role="progress" data-value="75" data-small="true" data-cls-bar="bg-white" data-cls-back="bg-darkCyan" data-role-progress="true" className="progress small bg-darkCyan"><div className="bar bg-white" style={{width: "75%"}}></div><span className="value" style={{display: "none", left: "75%"}}>75%</span></div>
              <div className="pl-2 pr-2">
                <span className="text-small">
                  70% Increase in 30 Days
                </span>
              </div>
            </div>
          </div>
        </div>
        <div className="cell-md-4 mt-4">
          <div className="icon-box bg-green fg-white">
            <div className="icon"><span className="mif-thumbs-up"></span></div>
            <div className="content">
              <div className="p-2">
                <div>LIKES</div>
                <div className="text-bold text-leader">41,400</div>
              </div>
              <div data-role="progress" data-value="75" data-small="true" data-cls-bar="bg-white" data-cls-back="bg-darkGreen" data-role-progress="true" className="progress small bg-darkGreen"><div className="bar bg-white" style={{width: "75%"}}></div><span className="value" style={{display: "none", left: "75%"}}>75%</span></div>
              <div className="pl-2 pr-2">
                <span className="text-small">
                  70% Increase in 30 Days
                </span>
              </div>
            </div>
          </div>
        </div>
        <div className="cell-md-4 mt-4">
          <div className="icon-box bg-orange fg-white">
            <div className="icon"><span className="mif-calendar"></span></div>
            <div className="content">
              <div className="p-2">
                <div>EVENTS</div>
                <div className="text-bold text-leader">41,400</div>
              </div>
              <div data-role="progress" data-value="75" data-small="true" data-cls-bar="bg-white" data-cls-back="bg-darkOrange" data-role-progress="true" className="progress small bg-darkOrange"><div className="bar bg-white" style={{width: "75%"}}></div><span className="value" style={{display: "none", left: "75%"}}>75%</span></div>
              <div className="pl-2 pr-2">
                <span className="text-small">
                  70% Increase in 30 Days
                </span>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}