using System;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using System.Collections.Generic;
using osum.Helpers;
using osum.Support.iPhone;
namespace osum
{
	public class InputSourceIphone : InputSource
	{
        public InputSourceIphone() : base()
		{
		}

        public void HandleTouches(NSSet touches)
        {
            Clock.Update(true);


            if (touches.Count == 1)
                handleUITouch((UITouch)touches.AnyObject);
            else
                foreach (UITouch u in touches.ToArray<UITouch>())
                    handleUITouch(u);
        }

        Dictionary<UITouch,TrackingPoint> touchDictionary = new Dictionary<UITouch, TrackingPoint>();

        private void handleUITouch(UITouch u)
        {

            TrackingPoint point = null;
            switch (u.Phase)
            {
                case UITouchPhase.Began:
                    if (AppDelegate.UsingViewController) return;
                    point = new TrackingPointIphone(u.LocationInView(EAGLView.Instance), u);
                    touchDictionary[u] = point;
                    trackingPoints.Add(point);
                    TriggerOnDown(point);
                    break;
                case UITouchPhase.Cancelled:
                case UITouchPhase.Ended:
                    point = touchDictionary[u];
                    trackingPoints.Remove(point);
                    touchDictionary.Remove(u);
                    TriggerOnUp(point);
                    break;
                case UITouchPhase.Moved:
                    point = touchDictionary[u];
                    if (point != null)
                    {
                        point.Location = u.LocationInView(EAGLView.Instance);
                        TriggerOnMove(point);
                    }
                    break;
            }
        }


        public void ReleaseAllTouches()
        {
            foreach (TrackingPoint t in trackingPoints)
                TriggerOnUp(t);
            trackingPoints.Clear();
            touchDictionary.Clear();
        }
	}
}

