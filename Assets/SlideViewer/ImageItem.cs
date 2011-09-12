using UnityEngine;
using System.Collections;

public class ImageItem : MonoBehaviour
{
    public void Init(string imagePath)
    {
        StartCoroutine(Load(imagePath));
    }

    IEnumerator Load(string imagePath)
    {
        // TODO path
        WWW req = new WWW("file://" + System.IO.Path.GetFullPath(imagePath).Replace('\\','/'));
        yield return req;
        transform.Find("Image").localScale = new Vector3((float)req.texture.width / (float)req.texture.height, 1.0f, 1.0f);
        transform.Find("Image").renderer.material.mainTexture = req.texture;
    }
}
