using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UITestAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UISetUpAttribute : Attribute
{
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class UITearDownAttribute : Attribute
{
}

public class UITest : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator RunTest(MethodInfo method)
    {
        yield return null; // wait for destroy to be executed

        yield return StartCoroutine(Run(typeof(UISetUpAttribute)));
        yield return StartCoroutine(InvokeMethod(method));
        yield return StartCoroutine(Run(typeof(UITearDownAttribute)));

        Destroy(gameObject);
    }

    protected IEnumerator InvokeMethod(MethodInfo method)
    {
        var enumerable = (IEnumerable) method.Invoke(this, null);
        if (enumerable != null)
        {
            foreach (YieldInstruction y in enumerable)
                yield return y;
        }
    }

    IEnumerator Run(Type type)
    {
        foreach(MethodInfo method in GetType().GetMethods())
        {
            if (Attribute.IsDefined(method, type))
                yield return StartCoroutine(InvokeMethod(method));
        }
    }

    const float WaitTimeout = 2;
    const float WaitIntervalFrames = 10;

    protected Coroutine WaitFor(Condition condition)
    {
        return StartCoroutine(WaitForInternal(condition, Environment.StackTrace));
    }
                
    protected Coroutine LoadScene(string name)
    {
        return StartCoroutine(LoadSceneInternal(name));
    }

    IEnumerator LoadSceneInternal(string name)
    {                      
        SceneManager.LoadScene(name);
        yield return WaitFor(new SceneLoaded(name));
    }

#if UNITY_EDITOR
    protected Coroutine LoadSceneByPath(string path)
    {
        return StartCoroutine(LoadSceneByPathInternal(path));
    }

    IEnumerator LoadSceneByPathInternal(string path)
    {                      
        UnityEditor.EditorApplication.LoadLevelInPlayMode(path);
        yield return WaitFor(new SceneLoaded(Path.GetFileNameWithoutExtension(path)));
    }
#endif

    protected Coroutine AssertLabel(string id, string text)
    {
        return StartCoroutine(AssertLabelInternal(id, text));
    }

    T FindUIElement<T>(string name) where T : Component
    {
        T e = FindUIElementOrNull<T>(name);
        if (e == null) throw new Exception(typeof(T) + " not found: " + name);
        return e;
    }

    T FindUIElementOrNull<T>(string name) where T : Component
    {
        var children = FindObjectsOfType<T>();
        foreach (T element in children)
        {
            if (element != null && element.name != null && element.name.Equals(name))
                return element;
        }
        return null;
    }

    protected Coroutine Press(string buttonName)
    {
        return StartCoroutine(PressInternal(buttonName));
    }

    protected Coroutine Press(GameObject o)
    {
        return StartCoroutine(PressInternal(o));
    }

    IEnumerator WaitForInternal(Condition condition, string stackTrace)
    {
        float time = 0;
        while (!condition.Satisfied())
        {
            if (time > WaitTimeout)
                throw new Exception("Operation timed out: " + condition + "\n" + stackTrace);            
            for (int i = 0; i < WaitIntervalFrames; i++) {
                time += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    IEnumerator PressInternal(string buttonName)
    {
        var buttonAppeared = new ObjectAppeared(buttonName);
        yield return WaitFor(buttonAppeared);
        yield return Press(buttonAppeared.o);
    }

    IEnumerator PressInternal(GameObject o)
    {
        yield return WaitFor(new ButtonAccessible(o));
        Debug.Log("Button pressed: " + o);
        ExecuteEvents.Execute(o, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);
        yield return null;
    }
                
    IEnumerator AssertLabelInternal(string id, string text)
    {
        yield return WaitFor(new LabelTextAppeared(id, text));
    }
                
    protected abstract class Condition
    {
        protected string param;
        protected string objectName;

        public Condition()
        {
        }

        public Condition(string param)
        {
            this.param = param;
        }

        public Condition(string objectName, string param)
        {
            this.param = param;
            this.objectName = objectName;
        }

        public abstract bool Satisfied();

        public override string ToString()
        {
            return GetType() + " '" + param + "'";
        }

        protected T FindUIElement<T>(string name) where T : Component
        {
            T e = FindUIElementOrNull<T>(name);
            if (e == null) throw new Exception(typeof(T) + " not found: " + name);
            return e;
        }

        protected T FindUIElementOrNull<T>(string name) where T : Component
        {
            var children = FindObjectsOfType<T>();
            foreach (T element in children)
            {
                if (element != null && element.name != null && element.name.Equals(name))
                    return element;
            }
            return null;
        }
    }

    protected class LabelTextAppeared : Condition
    {
        public LabelTextAppeared(string objectName, string param) : base(objectName, param) {}

        public override bool Satisfied()
        {
            return GetErrorMessage() == null;
        }

        string GetErrorMessage()
        {
            var go = GameObject.Find(objectName);
            if (go == null) return "Label object " + objectName + " does not exist";
            if (!go.activeInHierarchy) return "Label object " + objectName + " is inactive";
            var t = go.GetComponent<Text>();
            if (t == null) return "Label object " + objectName + " has no Text attached";
            if (t.text != param) return "Label " + objectName + "\n text expected: " + param + ",\n actual: " + t.text;
            return null;
        }

        public override string ToString()
        {
            return GetErrorMessage();
        }
    }

    protected class SceneLoaded : Condition
    {        
        public SceneLoaded(string param) : base (param) {}

        public override bool Satisfied()
        {
            return SceneManager.GetActiveScene().name == param;
        }
    }

    protected class ObjectAnimationPlaying : Condition
    {
        public ObjectAnimationPlaying(string objectName, string param) :base (objectName, param) {}

        public override bool Satisfied()
        {
            GameObject gameObject = GameObject.Find(objectName);
            return gameObject.GetComponent<Animation>().IsPlaying(param);
        }
    }

    protected class ObjectAppeared<T> : Condition where T : Component
    {
        public override bool Satisfied()
        {
            var obj = FindObjectOfType(typeof (T)) as T;
            return obj != null && obj.gameObject.activeInHierarchy;
        }
    }

    protected class ObjectDisappeared<T> : Condition where T : Component
    {
        public override bool Satisfied()
        {
            var obj = FindObjectOfType(typeof(T)) as T;
            return obj == null || !obj.gameObject.activeInHierarchy;
        }
    }

    protected class ObjectAppeared : Condition
    {
        protected string path;
        public GameObject o;

        public ObjectAppeared(string path)
        {
            this.path = path;
        }

        public override bool Satisfied()
        {
            o = GameObject.Find(path);
            return o != null && o.activeInHierarchy;
        }

        public override string ToString()
        {
            return "ObjectAppeared(" + path + ")";
        }
    }

    protected class ObjectDisappeared : ObjectAppeared
    {
        public ObjectDisappeared(string path) : base(path) {}

        public override bool Satisfied()
        {
            return !base.Satisfied();
        }

        public override string ToString()
        {
            return "ObjectDisappeared(" + path + ")";
        }
    }

    protected class BoolCondition : Condition
    {
        private Func<bool> _getter;

        public BoolCondition(Func<bool> getter)
        {
            _getter = getter;
        }

        public override bool Satisfied()
        {
            if (_getter == null) return false;
            return _getter();
        }

        public override string ToString()
        {
            return "BoolCondition(" + _getter + ")";
        }
    }

    protected class ButtonAccessible : Condition
    {
        GameObject button;

        public ButtonAccessible(GameObject button)
        {
            this.button = button;
        }

        public override bool Satisfied()
        {
            return GetAccessibilityMessage() == null;
        }

        public override string ToString()
        {
            return GetAccessibilityMessage() ?? "Button " + button.name + " is accessible";
        }

        string GetAccessibilityMessage()
        {
            if (button == null)
                return "Button " + button + " not found";
            if (button.GetComponent<Button>() == null)
                return "GameObject " + button + " does not have a Button component attached";
            return null;
        }
    }


    ////////////////////////////////////////////////////////////////////////////////////////
    //liziyi code
    //Pointer Down M
    protected Coroutine PointerDown(string controlName, Dir dir)
    {
        return StartCoroutine(PointerDownInternal(controlName, dir));
    }
    protected IEnumerator PointerDownInternal(string controlName, Dir dir)
    {
        var controlAppeared = new ObjectAppeared(controlName);
        Debug.Log("waiting1 ...");
        yield return WaitFor(controlAppeared);
        yield return PointerDownInternal(controlAppeared.o, dir);
    }
    protected IEnumerator PointerDownInternal(GameObject controlObj, Dir dir)
    {
        Debug.Log("waiting2 ...");
        yield return WaitFor(new PointerAccessible(controlObj, dir));
        Debug.Log("PointerDown: " + controlObj);
        ExecuteEvents.Execute(controlObj, new PointerEventData(EventSystem.current), ExecuteEvents.pointerDownHandler);
        yield return null;
    }

    //Pointer Up M
    protected Coroutine PointerUp(string controlName, Dir dir)
    {
        return StartCoroutine(PointerUpInternal(controlName, dir));
    }
    protected IEnumerator PointerUpInternal(string controlName, Dir dir)
    {
        var controlAppeared = new ObjectAppeared(controlName);
        Debug.Log("waiting1 ...");
        yield return WaitFor(controlAppeared);
        yield return PointerUpInternal(controlAppeared.o, dir);
    }
    protected IEnumerator PointerUpInternal(GameObject controlObj, Dir dir)
    {
        Debug.Log("waiting2 ...");
        yield return WaitFor(new PointerAccessible(controlObj, dir));
        Debug.Log("PointerUp: " + controlObj);
        ExecuteEvents.Execute(controlObj, new PointerEventData(EventSystem.current), ExecuteEvents.pointerUpHandler);
        yield return null;
    }

    //Pointer Drag 
    protected Coroutine PointerDrag(string controlName, Dir dir, Vector2 pos)
    {
        return StartCoroutine(PointerDragInternal(controlName, dir, pos));
    }
    protected IEnumerator PointerDragInternal(string controlName, Dir dir, Vector2 pos)
    {
        var controlAppeared = new ObjectAppeared(controlName);
        Debug.Log("waiting1 ...");
        yield return WaitFor(controlAppeared);
        yield return PointerDragInternal(controlAppeared.o, dir, pos);
    }
    protected IEnumerator PointerDragInternal(GameObject controlObj, Dir dir, Vector2 pos)
    {
        Debug.Log("waiting2 ...");
        yield return WaitFor(new PointerAccessible(controlObj, dir));

        //构造一个Pointer事件
        PointerEventData myPE = new PointerEventData(EventSystem.current);
        myPE.position = pos;
        Debug.Log("PointerDrag: " + controlObj);
        Debug.LogFormat("PointerDrag:{0},{1} ", pos.x, pos.y);
        ExecuteEvents.Execute(controlObj, myPE, ExecuteEvents.dragHandler);
        yield return null;
    }
    public enum Dir
    {
        left, right
    }

    protected class PointerAccessible : Condition
    {

        GameObject pointer;
        Dir curDir;
        /// <summary>
        /// 构造左边摇杆或者右边摇杆
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="dir"></param>
        public PointerAccessible(GameObject pointer, Dir dir)
        {
            this.pointer = pointer;
            curDir = dir;
        }

        public override bool Satisfied()
        {
            return GetAccessibilityMessage() == null;
        }

        public override string ToString()
        {
            return GetAccessibilityMessage() ?? "pointer " + pointer.name + " is accessible";
        }

        string GetAccessibilityMessage()
        {
            if (pointer == null)
                return "pointer " + pointer + " not found";

            if (curDir == Dir.left)//左摇杆
            {
                if (pointer.GetComponent<UnityStandardAssets.CrossPlatformInput.Joystick>() == null)
                    return "pointer L " + pointer + " does not have a Button component attached";
            }
            else//右摇杆
            {
                if (pointer.GetComponent<UnityStandardAssets.CrossPlatformInput.RightStickHandler>() == null)
                    return "pointer R " + pointer + " does not have a Button component attached";
            }


            return null;
        }
    }
    protected class ObjectActive : ObjectAppeared
    {

        public ObjectActive(string path) : base(path) { }

        public override bool Satisfied()
        {
            o = GameObject.Find(path);
            return o != null && o.activeInHierarchy;
        }

        public override string ToString()
        {
            return "ObjectAppeared(" + path + ")";
        }
    }
    //protected class ObjectDisActive : ObjectActive
    //{
    //    public ObjectDisActive(string path) : base(path) { }

    //    public override bool Satisfied()
    //    {
    //        return !base.Satisfied();
    //    }

    //    public override string ToString()
    //    {
    //        return "ObjectDisappeared(" + path + ")";
    //    }
    //}










    //liziyi code
    /// ////////////////////////////////////////////////////////////////////////

}
