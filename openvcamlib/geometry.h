#ifndef _GEOMETRY_H_
#define _GEOMETRY_H_

#include <opencv2/opencv.hpp>

using namespace std;
using namespace cv;


enum class Orientation {
    Colinear = 0,
    Clockwise = 1,
    CounterClockwise = 2
};

class Polygon {
private:
    // Given three colinear points p, q, r, the function checks if point q lies on line segment 'pr'
    static bool onSegment(Point segment_start, Point segment_end, Point p)
    {
        if (min(segment_start.x, segment_end.x) <= p.x <= max(segment_start.x, segment_end.x) && 
            min(segment_start.y, segment_end.y) <= p.y <= max(segment_start.y, segment_end.y))
             return(true);
        else
            return(false);
    }

    // To find orientation of ordered triplet (p, q, r).
    // https://www.geeksforgeeks.org/orientation-3-ordered-points/
    static Orientation orientation(Point p1, Point p2, Point p3)
    {
        int val = (p2.y - p1.y) * (p3.x - p2.x) -
                  (p2.x - p1.x) * (p3.y - p2.y);

        if (val == 0) 
            return (Orientation::Colinear);

        return (val > 0) ? Orientation::Clockwise : Orientation::CounterClockwise;
    }

    // The function that returns true if line segment 'p1q1' and 'p2q2' intersect.
    // https://www.geeksforgeeks.org/check-if-two-given-line-segments-intersect/
    static bool doIntersect(Point p1, Point q1, Point p2, Point q2)
    {
        // Find the four orientations needed for general and
        // special cases
        Orientation p1q1p2 = orientation(p1, q1, p2);
        Orientation p1q1q2 = orientation(p1, q1, q2);
        Orientation p2q2p1 = orientation(p2, q2, p1);
        Orientation p2q2q1 = orientation(p2, q2, q1);

        // General case
        if (p1q1p2 != p1q1q2 && p2q2p1 != p2q2q1)
            return(true);

        // Special Cases
        // p1, q1 and p2 are colinear and p2 lies on segment p1q1
        if (p1q1p2 == Orientation::Colinear && onSegment(p1, p2, q1))
            return(true);

        // p1, q1 and p2 are colinear and q2 lies on segment p1q1
        if (p1q1q2 == Orientation::Colinear && onSegment(p1, q2, q1))
            return(true);

        // p2, q2 and p1 are colinear and p1 lies on segment p2q2
        if (p2q2p1 == Orientation::Colinear && onSegment(p2, p1, q2))
            return (true);

        // p2, q2 and q1 are colinear and q1 lies on segment p2q2
        if (p2q2q1 == Orientation::Colinear && onSegment(p2, q1, q2))
            return (true);

        return(false); // Doesn't fall in any of the above cases
    }

public:
    // Returns true if the point p lies inside the polygon[] with n vertices
    // https://www.geeksforgeeks.org/how-to-check-if-a-given-point-lies-inside-a-polygon/
    static bool IsInside(vector<Point> polygon, Point p)
    {
        // There must be at least 3 vertices in polygon
        if (polygon.size() < 3)
            return(false);

        // Create a point for line segment from p to infinite
        Point extreme = { 10000, p.y }; //INT_MAX

        // Count intersections of the above line with sides of polygon
        int count = 0;
        for (int i = 0; i < (int)polygon.size(); i++)
        {
            int next = (i + 1) % (int)polygon.size();

            // Check if the line segment from 'p' to 'extreme' intersects
            // with the line segment from 'polygon[i]' to 'polygon[next]'
            if (doIntersect(polygon[i], polygon[next], p, extreme))
            {
                // If the point 'p' is colinear with line segment 'i-next', then
                // check if it lies on segment. If it lies, return true, otherwise false
                if (orientation(polygon[i], p, polygon[next]) == Orientation::Colinear)
                    return(onSegment(polygon[i], p, polygon[next]));

                count++;
            }
        }

        // Return true if count is odd, false otherwise
        return(count & 1); // (count % 2) == 1);
    }

    static bool IsInside(vector<Point> polygon, Rect r) {

        if (IsInside(polygon, cv::Point(r.x, r.y)))

            if (IsInside(polygon, cv::Point(r.x + r.width, r.y)))

                if (IsInside(polygon, cv::Point(r.x, r.y + r.height)))
        
                    if (IsInside(polygon, cv::Point(r.x + r.width, r.y + r.height)))

                        return(true);

        return(false);
    }
};

#endif