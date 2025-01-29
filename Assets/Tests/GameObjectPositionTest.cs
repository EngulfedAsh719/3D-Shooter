using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class GameObjectPositionTest
{
    [UnityTest]
    public IEnumerator GameObjectHasCorrectPosition()
    {
        var gameObject = new GameObject("TestObject");
        gameObject.transform.position = new Vector3(10, 0, 0);
        yield return null;
        Assert.AreEqual(new Vector3(10, 0, 0), gameObject.transform.position);
    }
}
