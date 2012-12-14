using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap;

public class LeapDevice : Leap.Listener
{
  

    public event System.Action<Controller> OnInit;
   
    public override void onInit(Controller controller)
    {
        Debug.Log("fully initialized controller");
        if (OnInit != null)
        {
            OnInit(controller);   
            
        }
    }

    public event System.Action<Controller> OnFrame;
    public override void onFrame(Controller controller)
    {
       // Debug.Log("onFrame");
        if (OnFrame != null)
        {
            OnFrame(controller);
        }

       
    }
    public event System.Action<Controller> OnConnect;
    public override void onConnect(Controller controller)
    {
         Debug.Log("onConnect");
        if (OnConnect != null)
        {
            OnConnect(controller);
        }


    }
    public event System.Action<Controller> OnDisconnect;
    public override void onDisconnect(Controller controller)
    {
        Debug.Log("onDisconect");
        if (OnDisconnect != null)
        {
            OnDisconnect(controller);
            

        }


    }


}

public class LeapInput : MonoBehaviour
{

    event System.Action<int> _OnHandFound;
    public static event System.Action<int> OnHandFound
    {
        add
        {
            instance._OnHandFound += value;
        }

        remove
        {
            instance._OnHandFound -= value;
        }
    }
    event System.Action<int, Hand> _OnHandUpdate;
    public static event System.Action<int, Hand> OnHandUpdate
    {
        add
        {
            instance._OnHandUpdate += value;
        }

        remove
        {
            instance._OnHandUpdate -= value;
        }
    }

    event System.Action<int> _OnHandLost;
    public static event System.Action<int> OnHandLost
    {
        add
        {
            instance._OnHandLost += value;
        }

        remove
        {
            instance._OnHandLost -= value;
        }
    }



    event System.Action<int, int> _OnFingerFound;
    public static event System.Action<int, int> OnFingerFound
    {
        add
        {
            instance._OnFingerFound += value;
        }

        remove
        {
            instance._OnFingerFound -= value;
        }
    }

    event System.Action<int, int, Finger> _OnFingerUpdate;
    public static event System.Action<int, int, Finger> OnFingerUpdate
    {
        add
        {
            instance._OnFingerUpdate += value;
        }

        remove
        {
            instance._OnFingerUpdate -= value;
        }
    }


    event System.Action<int> _OnFingerLost;
    public static event System.Action<int> OnFingerLost
    {
        add
        {
            instance._OnFingerLost += value;
        }

        remove
        {
            instance._OnFingerLost -= value;
        }
    }




    List<int> _activeHandIDs;
    List<int> _activeFingerIDs;
    public static List<int> activeHandIDs
    {
        get
        {
            return instance._activeHandIDs;
        }
    }
    public static List<int> activeFingerIDs
    {
        get
        {
            return instance._activeFingerIDs;
        }
    }
    
    Controller controller;
    LeapDevice leapDevice;
    static LeapInput _instance;
    static LeapInput instance
    {
        get
        {
            if (_instance == null)
            {
                
                //leapDevice.OnFrame += _instance.ProcessFrame;
                GameObject leap = new GameObject("Leap");
                _instance = leap.AddComponent<LeapInput>();
                //_instance.leapDevice = new LeapDevice();
                //_instance.leapDevice.OnFrame += _instance.ProcessFrame;
                _instance.controller = new Controller();
                _instance._activeHandIDs = new List<int>();
                _instance._activeFingerIDs = new List<int>();
            }
            return _instance;
        }
    }

    void OnDestroy()
    {
        _instance.controller = null;
    }

    public static Frame frame()
    {
        return instance.controller.frame();
    }
    void FixedUpdate()
    {
       // if (_instance.controller.frame())
       // {
            DoProcessFrame(_instance.controller);
        //    _instance.dpf = false;
       // }
    }
    //bool dpf = false;
  //  void ProcessFrame(Controller controller)
  //  {      
   //     _instance.dpf = true;
  //  }
    void DoProcessFrame(Controller controller)
    {
        //Debug.Log("Processing Frame...");
        //catalog hand IDs:
        List<int> currentHandsIDs = new List<int>();
        List<int> currentFingersIDs = new List<int>();
        
        foreach (Hand hand in controller.frame().hands())
        {
            int id = hand.id();
            currentHandsIDs.Add(hand.id());
            if (!_activeHandIDs.Contains(id))
            {
                HandFound(id);
            }

            else if (_OnHandUpdate != null)
            {
                _OnHandUpdate(id, hand);
            }


            foreach (Finger finger in hand.fingers())
            {
                int fid = finger.id();
                currentFingersIDs.Add(fid);
                if (!_activeFingerIDs.Contains(fid))
                {
                    FingerFound(fid, id);
                }

                else if (_OnFingerUpdate != null)
                {
                    _OnFingerUpdate(fid, id, finger);
                }
            }
        }

        //lost hands?
        if (currentHandsIDs.Count < _activeHandIDs.Count)
        {
            List<int> deadHandIDs = new List<int>();
            foreach (int ID in _activeHandIDs)
            {
                if (!currentHandsIDs.Contains(ID))
                {
                    deadHandIDs.Add(ID);
                }
            }
            foreach (int deadID in deadHandIDs)
            {
                HandLost(deadID);
            }
        }

        //lost fingers?
        if (currentFingersIDs.Count < _activeFingerIDs.Count)
        {
            List<int> deadFingerIDs = new List<int>();
            foreach (int ID in _activeFingerIDs)
            {
                if (!currentFingersIDs.Contains(ID))
                {
                    deadFingerIDs.Add(ID);
                }
            }
            foreach (int deadID in deadFingerIDs)
            {
                FingerLost(deadID);
            }
        }        
    }
    

    void HandFound(int ID)
    {
        _activeHandIDs.Add(ID);
        if (_OnHandFound != null)
        {
            _OnHandFound(ID);
        }
    }

    void HandLost(int ID)
    {
        _activeHandIDs.Remove(ID);
        if (_OnHandLost != null)
        {
            _OnHandLost(ID);
        }
    }

    void FingerFound(int ID, int handID)
    {
        _activeFingerIDs.Add(ID);
        if (_OnFingerFound != null)
        {
            _OnFingerFound(ID, handID);
        }
    }

    void FingerLost(int ID)
    {
        _activeFingerIDs.Remove(ID);
        if (_OnFingerLost != null)
        {
            _OnFingerLost(ID);
        }
    }

}