using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;


namespace LunaLyrics.Util
{
    public readonly struct PoolOption
    {
        public readonly Transform ObjectParent;
        public readonly string ObjectName;
        public readonly int InitialCount;

        public PoolOption(Transform objectParent, string objectName, int initialCount)
        {
            ObjectParent = objectParent;
            ObjectName = objectName;
            InitialCount = initialCount;
        }
    }

    public class ObjectPool<T> where T : Component
    {
        private readonly Stack<T> _poolStack;
        private readonly List<T> _popList;

        private readonly T _oriObject;
        private readonly PoolOption _options;
        private readonly Action<T> _initObjectAction;
        private readonly Action<T> _releaseObjectAction;

        private int _allocCount;

        public ObjectPool(T oriObject, PoolOption options, Action<T> initObjectAction, Action<T> releaseObjectAction)
        {
            _poolStack = new Stack<T>();
            _popList = new List<T>();
            _oriObject = oriObject;
            _options = options;
            _allocCount = 0;

            _initObjectAction = initObjectAction;
            _releaseObjectAction = releaseObjectAction;

            Allocate(_options.InitialCount);
        }

        public T Pop()
        {
            if (_poolStack.Count == 0)
                Allocate(1);

            var popObject = _poolStack.Pop();
            popObject.gameObject.SetActive(true);
            _popList.Add(popObject);
            _initObjectAction?.Invoke(popObject);

            return popObject;
        }

        public void Push(T obj)
        {
            obj.transform.SetParent(_options.ObjectParent);
            obj.gameObject.SetActive(false);

            if (_poolStack.Contains(obj))
                return;

            _releaseObjectAction?.Invoke(obj);
            _popList.Remove(obj);
            _poolStack.Push(obj);
        }

        public void Push(T[] objs)
        {
            foreach (var obj in objs)
                Push(obj);
        }

        public void Clear()
        {
            if (_popList.Count == 0)
                return;

            Push(_popList.ToArray());
        }

        private void Allocate(int allocCount)
        {
            for (var i = 0; i < allocCount; ++i)
            {
                var newObj = Object.Instantiate(_oriObject, _options.ObjectParent, false);
                newObj.name = _options.ObjectName + _allocCount;
                newObj.gameObject.SetActive(false);

                _poolStack.Push(newObj);
                ++_allocCount;
            }
        }
    }
}