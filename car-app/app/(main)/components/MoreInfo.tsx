'use client'; // Required for client-side window access

import Link from 'next/link';
import { useEffect } from 'react';

export default function MoreInfo() {

  return (
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
          <Link href="/windows" className="more"> More info <span className="mif-arrow-right"></span></Link>
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
  );
}