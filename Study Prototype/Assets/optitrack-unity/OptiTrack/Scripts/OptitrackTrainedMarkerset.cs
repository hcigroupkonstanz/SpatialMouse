/* 
Copyright © 2016 NaturalPoint Inc.

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License. 
*/

using System;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Implements live tracking of streamed OptiTrack trained markerset data onto an asset in Unity.
/// </summary>
/// <remarks>
/// A hierarchy of GameObjects (see <see cref="m_rootObject"/> and <see cref="m_boneObjectMap"/>) will be created to
/// receive the streaming pose data for the tmarkerset asset specified by <see cref="TMarkersetAssetName"/>.
/// </remarks>

public class OptitrackTrainedMarkerset : MonoBehaviour
{
    /// <summary>The client object to use for receiving streamed TMarkerset pose data.</summary>
    [Tooltip("The object containing the OptiTrackStreamingClient script.")]
    public OptitrackStreamingClient StreamingClient;

    /// <summary>The name of the TMarkerset asset in the stream that will provide retargeting source data.</summary>
    [Tooltip("The name of markerset asset in Motive.")]
    public string TMarkersetAssetName = "TMarkerset1";

    #region Private fields
    /// <summary>The streamed source tmarkerset definition.</summary>
    private OptitrackTMarkersetDefinition m_tmarkersetDef;

    /// <summary>The root GameObject of the streamed tmarkerset pose transform hierarchy.</summary>
    private GameObject m_rootObject;

    /// <summary>Maps between OptiTrack tmarkerset bone IDs and corresponding GameObjects.</summary>
    private Dictionary<Int32, GameObject> m_boneObjectMap;

    /// <summary>
    /// Maps between game object's bone names (keys) and streamed bone names from OptiTrack software (values).
    /// </summary>
    private Dictionary<string, Transform> m_cachedBoneNameMap = new Dictionary<string, Transform>(); // Optitrack's skeleton's bone names

    private Dictionary<Transform, Transform> m_transformMap = new Dictionary<Transform, Transform>();

    #endregion Private fields


    void Start()
    {
        // If the user didn't explicitly associate a client, find a suitable default.
        if (this.StreamingClient == null)
        {
            this.StreamingClient = OptitrackStreamingClient.FindDefaultClient();

            // If we still couldn't find one, disable this component.
            if (this.StreamingClient == null)
            {
                Debug.LogError(GetType().FullName + ": Streaming client not set, and no " + typeof(OptitrackStreamingClient).FullName + " components found in scene; disabling this component.", this);
                this.enabled = false;
                return;
            }
        }

        this.StreamingClient.RegisterTMarkerset(this, this.TMarkersetAssetName);

        // Retrieve the OptiTrack tmarkerset definition.
        m_tmarkersetDef = this.StreamingClient.GetTMarkersetDefinitionByName(this.TMarkersetAssetName);

        if (m_tmarkersetDef == null)
        {
            Debug.LogError(GetType().FullName + ": Could not find trained markerset definition with the name \"" + this.TMarkersetAssetName + "\"", this);
            this.enabled = false;
            return;
        }


        // Create a hierarchy of GameObjects that will receive the tmarkerset pose data
        string rootObjectName = "OptiTrack TMarkerset - " + this.TMarkersetAssetName;
        m_rootObject = new GameObject( rootObjectName );
        
        m_boneObjectMap = new Dictionary<Int32, GameObject>( m_tmarkersetDef.Bones.Count );

        for (int boneDefIdx = 0; boneDefIdx < m_tmarkersetDef.Bones.Count; boneDefIdx++)
        {
            OptitrackTMarkersetDefinition.BoneDefinition boneDef = m_tmarkersetDef.Bones[boneDefIdx];

            GameObject boneObject = new GameObject(boneDef.Name);
            if (boneDef.ParentId == -1) { boneObject.name = "Root"; } // set the parent name to 'Root' to match the naming in dictionary

            boneObject.transform.parent = boneDef.ParentId == -1 ? m_rootObject.transform : m_boneObjectMap[boneDef.ParentId].transform; // parent ID starts at -1 in TM
            boneObject.transform.localPosition = boneDef.Offset;
            m_boneObjectMap[boneDef.Id] = boneObject;
            //Debug.Log("boneDef: " + boneObject.name + " " + boneObject.transform.name); // exact same
            m_cachedBoneNameMap[boneObject.transform.name] = boneObject.transform;
        }

        Setup(rootObjectName);

        m_rootObject.transform.parent = this.StreamingClient.transform;
        m_rootObject.transform.localPosition = Vector3.zero;
        m_rootObject.transform.localRotation = Quaternion.identity;
    }

    private void Update()
    {
        OptitrackTMarkersetState tmarState = StreamingClient.GetLatestTMarkersetState( m_tmarkersetDef.Id );
        if (tmarState != null)
        {
            // Update the transforms of the bone GameObjects.
            for (int i = 0; i < m_tmarkersetDef.Bones.Count; ++i)
            {
                Int32 boneId = m_tmarkersetDef.Bones[i].Id;

                OptitrackPose bonePose;
                GameObject boneObject;

                bool foundPose = false;
                if (StreamingClient.TMarkersetCoordinates == StreamingCoordinatesValues.Global)
                {
                    // Use global tmarkerset coordinates
                    foundPose = tmarState.LocalBonePoses.TryGetValue(boneId, out bonePose);
                }
                else
                {
                    // Use local tmarkerset coordinates
                    foundPose = tmarState.BonePoses.TryGetValue(boneId, out bonePose);
                }

                bool foundObject = m_boneObjectMap.TryGetValue(boneId, out boneObject);
                if (foundPose && foundObject)
                {
                    boneObject.transform.localPosition = bonePose.Position;
                    boneObject.transform.localRotation = bonePose.Orientation;
                    m_transformMap[boneObject.transform].transform.localPosition = bonePose.Position;
                    m_transformMap[boneObject.transform].transform.localRotation = bonePose.Orientation;
                }
            }
        }
    }

    #region Private methods
    /// <summary>
    /// Constructs the source to target mapping of the bones
    /// </summary>
    /// <param name="rootObjectName"></param>
    private void Setup(string rootObjectName)
    {
        // Set up the mapping between destination Game Object and hierarchy of GameObjects we created with the source streamed data
        //Debug.Log("name of gameobject: " + gameObject.name);

        GameObject srcObject = GameObject.Find(rootObjectName);
        Transform[] srcObjectBones = srcObject.GetComponentsInChildren<Transform>(); // source

        Transform[] tarObjectBones = this.GetComponentsInChildren<Transform>(); // target

        // Iterate through the bones in source and map onto the destination
        foreach (var bone in tarObjectBones)
        {
            if (bone.name.EndsWith("End")) 
            {
                ;
            }
            else
            {
                if (m_cachedBoneNameMap.ContainsKey(bone.name) == false)
                {
                    Debug.Log(bone.name + " name exists in target, but does not exist in the source.");
                }
                else
                {
                    m_transformMap[m_cachedBoneNameMap[bone.name]] = bone;
                }
            }
            
        }
    }
    #endregion Private methods
}