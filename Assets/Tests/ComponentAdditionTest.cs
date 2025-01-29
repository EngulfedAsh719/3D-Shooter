using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class ComponentAdditionTest
{
    [UnityTest]
    public IEnumerator RigidbodyComponentIsAdded()
    {
        var gameObject = new GameObject("TestObject");
        var rigidbody = gameObject.AddComponent<Rigidbody>();
        yield return null;
        Assert.IsNotNull(rigidbody);
    }
}
