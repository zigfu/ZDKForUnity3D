using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using Zigfu.FaceTracking;
using Zigfu.Utility;        // For MathHelper.ConvertFromRangeToRange


[RequireComponent(typeof(MorphableFace))]


// Summary:
//      Natural User Interface (NUI) for the MorphableFace.
//       This class maps the user's actual facial expressions 
//       (as interpreted by the ZigFaceTracker and quantified in AnimationUnits)
//       to the model's facial expressions (controlled via the MorphableFace.MorphableFeatures).
//
public class MorphableFace_NUI : MonoBehaviour
{
    public float smoothing = 0.6f;


    MorphableFace _morphableFace;


    // Summary: 
    //      The NUIFeature class facillitates the conversion between 
    //       a ZigFaceTracker AnimationUnit (AU) and
    //       a MorphableFace.MorphableFeature (MF).
    class NUIFeature
    {
        public readonly AnimationUnit au;
        public readonly MorphableFace.MorphableFeature mf;

        float _minAUCoeff, _maxAUCoeff;


        public static NUIFeature CreateNew(AnimationUnit au, MorphableFace.MorphableFeature mf, float minAUCoeff, float maxAUCoeff) 
        {
            return new NUIFeature(au, mf, minAUCoeff, maxAUCoeff);
        }

        public NUIFeature(AnimationUnit au, MorphableFace.MorphableFeature mf, float minAUCoeff, float maxAUCoeff)
        {
            this.au = au;
            this.mf = mf;
            this._minAUCoeff = minAUCoeff;
            this._maxAUCoeff = maxAUCoeff;
        }

        public float AUCoeff_2_MFCoeff(float auCoeff)
        {
            return MathHelper.ConvertFromRangeToRange(
                _minAUCoeff, _maxAUCoeff,
                MorphableFace.MinMorphCoeff, MorphableFace.MaxMorphCoeff,
                auCoeff);
        }
    }

    Dictionary<AnimationUnit, NUIFeature> _nuiFeatures;


    void Awake()
    {
        InitNUIFeatures();
    }

    void InitNUIFeatures()
    {
        // Note: The values for minAUCoeff and maxAUCoeff provided for each
        //  NUIFeature were determined through experimentation, and may require
        //  slight tweaks depending on how the FaceTracking behaves in a given environment
        //  or for a given person's face

        _nuiFeatures = new Dictionary<AnimationUnit, NUIFeature>()
        {
		    { AnimationUnit.BrowLower,          
                NUIFeature.CreateNew(
                    AnimationUnit.BrowLower, 
                    MorphableFace.MorphableFeature.BrowLower, 
                    0.0f, 0.4f) 
                    },
            { AnimationUnit.BrowRaiser,          
                NUIFeature.CreateNew(
                    AnimationUnit.BrowRaiser, 
                    MorphableFace.MorphableFeature.BrowRaiser, 
                    -0.1f, 0.4f)
                    },
            { AnimationUnit.JawLower,          
                NUIFeature.CreateNew(
                    AnimationUnit.JawLower, 
                    MorphableFace.MorphableFeature.JawLower, 
                    0.1f, 0.8f) 
                    },
            { AnimationUnit.LipCornerDepressor,          
                NUIFeature.CreateNew(
                    AnimationUnit.LipCornerDepressor, 
                    MorphableFace.MorphableFeature.LipCornerDepressor, 
                    -0.2f, 1.0f) 
                    },
            { AnimationUnit.LipRaiser,          
                NUIFeature.CreateNew(
                    AnimationUnit.LipRaiser, 
                    MorphableFace.MorphableFeature.LipRaiser, 
                    -1.0f, -0.2f) 
                    },
            { AnimationUnit.LipStretcher,          
                NUIFeature.CreateNew(
                    AnimationUnit.LipStretcher, 
                    MorphableFace.MorphableFeature.LipStretcher, 
                    -1.0f, 1.0f) 
                    },
	    };
    }

    void Start () 
    {
        _morphableFace = gameObject.GetComponent<MorphableFace>() as MorphableFace;

        ZigInput.Instance.AddListener(gameObject);
	}


    void Zig_Update(ZigInput input)
    {
        if (!enabled) { return; }

        UpdateMorphableFace();
	}

    void UpdateMorphableFace()
    {
        // Get the FaceTracker's latest AnimationUnitCoefficients,
        //  and use them to update the associated MorphableFace's MorphCoefficients
        var auCoeffs = ZigFaceTracker.Instance.FaceTrackFrame.GetAnimationUnitCoefficients();
        if (auCoeffs == null) { return; }

        ZigFaceTracker.PrintAnimCoefs(auCoeffs);

        foreach (NUIFeature feature in _nuiFeatures.Values)
        {
            MorphableFace.MorphableFeature mf = feature.mf;
            AnimationUnit au = feature.au;

            float auCoeff = auCoeffs[au];
            float oldMorphCoeff = _morphableFace.GetMorphCoefficient(mf);
            float targetMorphCoeff = feature.AUCoeff_2_MFCoeff(auCoeff);

            float lerpAmt = 1 - Mathf.Clamp(smoothing, 0, 1);
            float mfCoeff = Mathf.Lerp(oldMorphCoeff, targetMorphCoeff, lerpAmt);

            _morphableFace.SetMorphCoefficient(mf, mfCoeff);
        }
    }

}
