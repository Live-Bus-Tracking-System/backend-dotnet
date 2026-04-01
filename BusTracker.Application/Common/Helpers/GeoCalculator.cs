namespace BusTracker.Application.Common.Helpers
{
    public static class GeoCalculator
    {
        private const double EarthRadiusMeters = 6371000;

        // Haversine Distance Formula
        public static double GetDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = (lat2 - lat1) * Math.PI / 180.0;
            var dLon = (lon2 - lon1) * Math.PI / 180.0;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return EarthRadiusMeters * (2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a)));
        }

        // Trajectory/Bearing Calculation (0 to 360 degrees)
        public static double GetBearing(double startLat, double startLon, double endLat, double endLon)
        {
            var lat1 = startLat * Math.PI / 180.0;
            var lat2 = endLat * Math.PI / 180.0;
            var dLon = (endLon - startLon) * Math.PI / 180.0;

            var y = Math.Sin(dLon) * Math.Cos(lat2);
            var x = Math.Cos(lat1) * Math.Sin(lat2) -
                    Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

            var bearing = Math.Atan2(y, x) * 180.0 / Math.PI;
            return (bearing + 360) % 360; // Normalize to 0-360
        }

        // O(1) Bounding Box Check (Is the bus even near this route?)
        public static bool IsInsideBoundingBox(double lat, double lon, double minLat, double maxLat, double minLon, double maxLon)
        {
            // added a tiny buffer (~500 meters) to the box so it don't miss edge cases
            const double buffer = 0.005;
            return lat >= (minLat - buffer) && lat <= (maxLat + buffer) &&
                   lon >= (minLon - buffer) && lon <= (maxLon + buffer);
        }

        // Distance from a point (the stop) to the bus's movement line segment
        public static double GetMinDistanceToLineSegment(double pointLat, double pointLon, double lineStartLat, double lineStartLon, double lineEndLat, double lineEndLon)
        {
            double metersPerDegreeLat = 111320.0;
            double metersPerDegreeLon = 111320.0 * Math.Cos(pointLat * Math.PI / 180.0);
            
            double px = (pointLon - lineStartLon) * metersPerDegreeLon;
            double py = (pointLat - lineStartLat) * metersPerDegreeLat;
            
            double sx = (lineEndLon - lineStartLon) * metersPerDegreeLon;
            double sy = (lineEndLat - lineStartLat) * metersPerDegreeLat;
            
            double segmentLengthSquared = sx * sx + sy * sy;
            
            if (segmentLengthSquared == 0) return GetDistanceMeters(pointLat, pointLon, lineStartLat, lineStartLon);
            
            double t = Math.Max(0, Math.Min(1, (px * sx + py * sy) / segmentLengthSquared));
            
            double closestX = t * sx;
            double closestY = t * sy;
            
            double distanceSq = (px - closestX) * (px - closestX) + (py - closestY) * (py - closestY);
            return Math.Sqrt(distanceSq);
        }

        // Decode OSRM/Google encoded polyline string into coordinates
        public static List<BusTracker.Application.Tracking.Models.RoutePolylinePoint> DecodePolyline(string encodedPolyline)
        {
            if (string.IsNullOrEmpty(encodedPolyline))
                return new List<BusTracker.Application.Tracking.Models.RoutePolylinePoint>();

            var poly = new List<BusTracker.Application.Tracking.Models.RoutePolylinePoint>();
            int index = 0, len = encodedPolyline.Length;
            int lat = 0, lng = 0;
            double accumulatedDistance = 0;
            BusTracker.Application.Tracking.Models.RoutePolylinePoint? previousPoint = null;

            while (index < len)
            {
                int b, shift = 0, result = 0;
                do
                {
                    b = encodedPolyline[index++] - 63;
                    result |= (b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);
                int dlat = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lat += dlat;

                shift = 0;
                result = 0;
                do
                {
                    b = encodedPolyline[index++] - 63;
                    result |= (b & 0x1f) << shift;
                    shift += 5;
                } while (b >= 0x20);
                int dlng = ((result & 1) != 0 ? ~(result >> 1) : (result >> 1));
                lng += dlng;

                var point = new BusTracker.Application.Tracking.Models.RoutePolylinePoint
                {
                    Latitude = lat / 1E5,
                    Longitude = lng / 1E5
                };

                if (previousPoint != null)
                {
                    var dist = GetDistanceMeters(previousPoint.Latitude, previousPoint.Longitude, point.Latitude, point.Longitude);
                    accumulatedDistance += dist;
                }
                
                point.AccumulatedDistanceMeters = accumulatedDistance;
                poly.Add(point);
                previousPoint = point;
            }

            return poly;
        }

        // Find the closest point on the line string and return its accumulated distance
        public static double SnapToPolyline(double lat, double lon, List<BusTracker.Application.Tracking.Models.RoutePolylinePoint> polyline)
        {
            return SnapToPolylineWithDistance(lat, lon, polyline).AccumulatedDistanceMeters;
        }

        public static (double AccumulatedDistanceMeters, double OffLineDistanceMeters) SnapToPolylineWithDistance(double lat, double lon, List<BusTracker.Application.Tracking.Models.RoutePolylinePoint> polyline)
        {
            if (polyline == null || !polyline.Any()) return (0, 0);
            if (polyline.Count == 1) return (0, GetDistanceMeters(lat, lon, polyline[0].Latitude, polyline[0].Longitude));

            double minDistance = double.MaxValue;
            double accumulatedDistanceAtSnap = 0;

            for (int i = 0; i < polyline.Count - 1; i++)
            {
                var p1 = polyline[i];
                var p2 = polyline[i + 1];

                // Calculate projection of point (lat, lon) onto the segment p1-p2
                double metersPerDegreeLat = 111320.0;
                double metersPerDegreeLon = 111320.0 * Math.Cos(lat * Math.PI / 180.0);

                double px = (lon - p1.Longitude) * metersPerDegreeLon;
                double py = (lat - p1.Latitude) * metersPerDegreeLat;

                double sx = (p2.Longitude - p1.Longitude) * metersPerDegreeLon;
                double sy = (p2.Latitude - p1.Latitude) * metersPerDegreeLat;

                double segmentLengthSquared = sx * sx + sy * sy;

                double t = segmentLengthSquared == 0 ? 0 : Math.Max(0, Math.Min(1, (px * sx + py * sy) / segmentLengthSquared));

                double closestX = t * sx;
                double closestY = t * sy;

                double distanceSq = (px - closestX) * (px - closestX) + (py - closestY) * (py - closestY);

                if (distanceSq < minDistance)
                {
                    minDistance = distanceSq;
                    var segmentDist = Math.Sqrt(segmentLengthSquared);
                    accumulatedDistanceAtSnap = p1.AccumulatedDistanceMeters + (t * segmentDist);
                }
            }

            return (accumulatedDistanceAtSnap, Math.Sqrt(minDistance));
        }
    }
}