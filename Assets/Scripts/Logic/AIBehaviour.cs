using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using StarterAssets;
using UnityEngine.Events;

public class AIBehaviour : MonoBehaviour
{
    [SerializeField] float distanceToStop = 1.8f;
    [SerializeField] Transform pathViewPoint;
    [SerializeField] Transform foundPathPointPrefab;

    [Space(18)]

    [SerializeField] Transform blockCheckPointDown;

    public float delay = 3f;

    MoveComponent moveComponent;
    [SerializeField] Character target;

    [SerializeField] List<Vector3> path = new();
    [SerializeField] Dictionary<Vector3, PathPoint> explored = new();

    List<GameObject> points = new();
    List<GameObject> pathPoints = new();

    UnityEvent onPathBuilded;

    PathPoint lastPoint;

    Vector3 checkPoint;

    float lifeTime;
    bool startPathfinding;
    bool pathFound;

    private void Start()
    {
        moveComponent = GetComponent<MoveComponent>();
        target = FindObjectsOfType<Character>().ToList().Find(p => p.GetComponent<ThirdPersonController>());
    }

    private void Update()
    {
        lifeTime += Time.deltaTime;

        if (lifeTime < 5)
            return;


        //if (Input.GetKeyDown(KeyCode.O))
        //    //CheckLandFrontLeg();
        //    //CheckNeedPathfinding();
        //    Pathfinding();

        if (Input.GetKeyDown(KeyCode.O))
        {
            Pathfinding();
        }

        var distance = Vector3.Distance(target.transform.position, transform.position);
        if (distanceToStop < distance)
        {
            if (CheckNeedPathfinding())
            {
                seeTimer = 0;
                moveComponent.Movenment = Vector3.zero;

                if (!startPathfinding && path.Count == 0)
                {
                    startPathfinding = true;
                    Pathfinding();
                }
                else
                {
                    if (path.Count > 0)
                    {
                        var point = path.Last();
                        var origin = transform.position;
                        origin.y = point.y;
                        var dist = Vector3.Distance(point, origin);
                        if(dist > 0.1f)
                        {
                            Move(point);
                            CheckLandFrontLeg();
                        }
                        else
                        {
                            path.Remove(point);
                        }
                    }
                    else
                    {
                        if (pathFound)
                        {
                            startPathfinding = false;
                        }
                    }
                }

            }
            else
            {
                seeTimer += Time.deltaTime;
                if (seeTimer > 1.8f)
                {
                    startPathfinding = false;
                    path.Clear();
                    Move();

                }
                if (seeTimer > 0.8f)
                {
                    //Move();
                }
            }
        }
        else
        {
            startPathfinding = false;
            path.Clear();
            moveComponent.Movenment = Vector3.zero;
        }


        checkPointTimer += Time.deltaTime;
        if (checkPointTimer > 5 && moveComponent.Movenment.magnitude > 0.01f)
        {
            if (Vector3.Distance(checkPoint, transform.position) < 1)
            {
                if (tryedJump)
                {
                    path.Clear();
                    Pathfinding();
                    print("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                    tryedJump = false;
                }
                else
                {
                    tryedJump = true;
                    moveComponent.Jump = true;
                }
            }

            checkPointTimer = 0;
            checkPoint = transform.position;
        }
    }

    float checkPointTimer;
    float seeTimer;
    bool tryedJump;

    private void Find_path(PathPoint start_node, PathPoint goal_node)
    {
        StartCoroutine(Async());

        IEnumerator Async()
        {
            List<PathPoint> reachable = new() { start_node };
            List<PathPoint> explored = new();

            PathPoint node;
            int speed = 3;
            int iteration = 0;

            points.ForEach(p => Destroy(p));
            points.Clear();

            while (reachable.Count > 0)
            {
                //print(goal_node.point);
                // Choose some node we know how to reach.
                node = choose_node(reachable, goal_node);
                points.Add(Instantiate(pathViewPoint, node.point, Quaternion.identity).gameObject);

                var distToGoal = Vector3.Distance(node.point, goal_node.point);
                // If we just got to the goal node, build and return the path.
                if (node.point == goal_node.point || distToGoal < distanceToStop)
                {
                    print("!!!!!!  Путь найден  !!!!!");
                    BuildPath(node);
                    break;
                }

                // Don't repeat ourselves.
                var removable = reachable.Find(n => n.point == node.point);
                reachable.Remove(removable);
                explored.Add(node);
                //print(reachable.Count);

                // Where can we get from here that we haven't explored before?
                List<PathPoint> new_reachable = get_adjacent_nodes(node, goal_node).FindAll(n => explored.Find(p => p.point == n.point) == null); //get_adjacent_nodes(node) - explored

                foreach (var adjacent in new_reachable)
                {
                    // First time we see this node?
                    if (!reachable.Any(n => n.point == adjacent.point))
                    {
                        reachable.Add(adjacent);
                    }

                    // If this is a new path, or a shorter path than what we have, keep it.
                    if (node.cost + 1 < adjacent.cost)
                    {
                        adjacent.previous = node;
                        adjacent.cost = node.cost + 1;
                    }
                }

                iteration++;
                if (iteration > speed)
                {
                    iteration = 0;
                    yield return null;
                }
                //yield return new WaitForSeconds(delay);
            }
            print("циклу пизда");

            if (path.Count == 0)
            {
                yield return new WaitForSeconds(3f);

                Pathfinding();
            }
            //# If we get here, no path was found :(
        }
    }

    List<PathPoint> get_adjacent_nodes(PathPoint node, PathPoint goalNode)
    {
        var generator = WorldGenerator.Inst;
        List<PathPoint> adjacents = new();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    var offset = new Vector3(x, y, z);

                    if (offset == Vector3.zero)
                        continue;

                    var newPos = node.point + offset;
                    var blockID = generator.GetBlockID(newPos);
                    var bottomBlock = generator.GetBlockID(newPos + Vector3.down);
                    var topBlock = generator.GetBlockID(newPos + (Vector3.up * 1.01f));
                    if (blockID == 0 && bottomBlock > 0 && topBlock == 0/**/)
                    {
                        var dist = Vector3.Distance(goalNode.point, newPos);
                        var adjacent = new PathPoint(newPos, dist);
                        adjacents.Add(adjacent);
                    }
                }
            }
        }

        return adjacents;
    }

    PathPoint choose_node(List<PathPoint> reachable, PathPoint goal_node)
    {
        //PathPoint nearest = null;
        //float minDist = float.MaxValue;
        //foreach (var node in nodes)
        //{
        //    if(node.distance < minDist)
        //    {
        //        minDist = node.distance;
        //        nearest = node;
        //    }
        //}

        //return nearest;

        float min_cost = float.MaxValue;
        PathPoint best_node = null;
        foreach (var node in reachable)
        {
            var cost_start_to_node = node.cost;
            var cost_node_to_goal = Vector3.Distance(node.point, goal_node.point);
            var total_cost = cost_start_to_node + cost_node_to_goal;

            if (min_cost > total_cost)
            {
                min_cost = total_cost;
                best_node = node;
            }
        }

        return best_node;
    }

    private void Pathfinding()
    {
        var generator = WorldGenerator.Inst;

        pathFound = false;

        var globalPos = transform.position;
        int xIdx = Mathf.FloorToInt(globalPos.x);
        int zIdx = Mathf.FloorToInt(globalPos.z);
        int yIdx = Mathf.FloorToInt(globalPos.y);// + 1;
        var key = new Vector3(xIdx, yIdx, zIdx);

        while (generator.GetBlockID(key + (Vector3.down * 0.3f)) == 0)
        {
            key += Vector3.down * 0.3f;
        }

        var dist = Vector3.Distance(transform.position, target.transform.position);
        var startNode = new PathPoint(key, dist) { cost = 0 };

        globalPos = target.transform.position;
        xIdx = Mathf.FloorToInt(globalPos.x) + 1;
        zIdx = Mathf.FloorToInt(globalPos.z);
        yIdx = Mathf.FloorToInt(globalPos.y + 0.3f);// + 1;
        key = new Vector3(xIdx, yIdx, zIdx);

        while (generator.GetBlockID(key + Vector3.down) == 0)
        {
            key += Vector3.down;
        }

        var goalNode = new PathPoint(key, 0);

        Find_path(startNode, goalNode);
        //StartCoroutine(Async());

        //IEnumerator Async()
        //{
        //    var distToTarget = Vector3.Distance(point.point, target.transform.position);

        //    while (distToTarget > distanceToStop)
        //    {
        //        List<PathPoint> neighbours = new();

        //        for (int x = -1; x <= 1; x++)
        //        {
        //            for (int y = -1; y <= 1; y++)
        //            {
        //                for (int z = -1; z <= 1; z++)
        //                {
        //                    if (x == 0 && z == 0)
        //                        continue;

        //                    var checkKey = new Vector3(key.x + x, key.y + y, key.z + z);
        //                    var blockID = generator.GetBlockID(checkKey);
        //                    var bottomBlock = generator.GetBlockID(checkKey + Vector3.down);
        //                    var topBlock = generator.GetBlockID(checkKey + (Vector3.up * 1.0f));
        //                    if (blockID == 0 && bottomBlock > 0 && topBlock == 0/**/)
        //                    {
        //                        var dist = Vector3.Distance(target.transform.position, checkKey);
        //                        var adjacent = new PathPoint(checkKey, dist);
        //                        //print(dist);
        //                        neighbours.Add(adjacent);
        //                    }
        //                }
        //            }
        //        }
        //        //print("======================================");
        //        neighbours = neighbours.FindAll(n => !explored.ContainsKey(n.point));
        //        var nextPoint = neighbours.Find(p => Mathf.Abs(p.distance - neighbours.Min(n => n.distance)) < 0.01f);
        //        if (nextPoint == null)
        //        {
        //            print("Ну все ебать");
        //            key = point.previous.point;
        //            point = point.previous;
        //            distToTarget = Vector3.Distance(point.point, target.transform.position);
        //        }
        //        else
        //        {
        //            if (point.cost + 1 < nextPoint.cost)
        //            {
        //                nextPoint.previous = point;
        //                nextPoint.cost = point.cost + 1;
        //            }

        //            key = nextPoint.point;
        //            //nextPoint.previous = point;
        //            point = nextPoint;
        //            explored.Add(key, point);
        //            Instantiate(pathViewPoint, point.point, Quaternion.identity);
        //            distToTarget = Vector3.Distance(point.point, target.transform.position);


        //        }



        //        yield return null;
        //        //yield return new WaitForSeconds(0.1f);
        //    }

        //    lastPoint = point;
        //    BuildPath();
        //}
    }

    void Move()
    {
        Move(target.transform.position);
        //var dir = target.transform.position - transform.position;
        //dir.y = 0;
        //transform.rotation = Quaternion.LookRotation(dir);

        //moveComponent.Movenment = new(transform.forward.x, transform.forward.z);

        CheckLandFrontLeg();
    }

    void Move(Vector3 target)
    {
        var dir = target - transform.position;
        dir.y = 0;
        transform.rotation = Quaternion.LookRotation(dir);

        moveComponent.Movenment = new(transform.forward.x, transform.forward.z);
    }


    bool CheckNeedPathfinding()
    {
        var posOrigin = transform.position + (Vector3.up * 1.1f);
        var posTarget = target.transform.position + (Vector3.up * 1.1f);
        var dir = (posTarget - posOrigin);//.normalized;

        if (Physics.Raycast(posOrigin, dir, out var hit))
        {
            var p = hit.collider.GetComponent<Character>();
            if(p && p == target)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        else
        {
            return false;
        }
    }

    void BuildPath(PathPoint node)
    {
        StartCoroutine(Async());

        IEnumerator Async()
        {
            pathPoints.ForEach(p => Destroy(p));
            pathPoints.Clear();

            while (node.previous != null)
            {
                var pos = node.point + (Vector3.left * 0.5f) + (Vector3.forward * 0.5f);
                path.Add(pos);
                pathPoints.Add(Instantiate(foundPathPointPrefab, node.point + (Vector3.one * 0.5f), Quaternion.identity).gameObject);

                node = node.previous;

                yield return null;
            }

            //startPathfinding = false;
            pathFound = true;
            onPathBuilded?.Invoke();
        }
    }


    void CheckLandFrontLeg()
    {
        var pos = (transform.forward * 0.7f) + transform.position + (Vector3.up * 0.8f);
        pos.x += 1;

        pos = blockCheckPointDown.position;
        pos.x += 1;

        var generator = WorldGenerator.Inst;

        var blockID = generator.GetBlockID(pos);
        //print(blockID);
        if(blockID > 0)
        {
            moveComponent.Jump = true;
        }
        else
        {
            blockID = generator.GetBlockID(pos + Vector3.up);
            if(blockID > 0)
            {
                moveComponent.Jump = true;
            }
        }


        //var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        //go.transform.position = pos;
    }
}

[System.Serializable]
public class PathPoint
{
    public Vector3 point;
    public PathPoint previous;
    public float distance;
    public int cost = 88888888;

    public PathPoint(Vector3 point, float distance)
    {
        this.point = point;
        this.distance = distance;
    }
}
