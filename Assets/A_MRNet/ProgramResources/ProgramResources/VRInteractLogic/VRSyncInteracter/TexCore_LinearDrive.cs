//======= Copyright (c) Valve Corporation, All rights reserved. ===============
//
// Purpose: Drives a linear mapping based on position between 2 positions
//
//=============================================================================

using UnityEngine;
using System.Collections;


	public class TexCore_LinearDrive : MonoBehaviour,IHandHoverUpdate
	{
		public Transform startPosition;
		public Transform endPosition;
		public TexCore_LinearMapping LinearMapping;
		public bool repositionGameObject = true;
		public bool maintainMomemntum = true;
		public float momemtumDampenRate = 5.0f;
		[SerializeField]

        protected float initialMappingOffset;
        protected int numMappingChangeSamples = 5;
        protected float[] mappingChangeSamples;
        protected float prevMapping = 0.0f;
        protected float mappingChangeRate;
        protected int sampleCount = 0;



        protected virtual void Awake()
        {
            mappingChangeSamples = new float[numMappingChangeSamples];
        }

        protected virtual void Start()
		{
           
            if ( LinearMapping == null )
			{
				LinearMapping = GetComponent<TexCore_LinearMapping>();
			}

			if ( LinearMapping == null )
			{
				LinearMapping = gameObject.AddComponent<TexCore_LinearMapping>();
			}

            initialMappingOffset = LinearMapping.Value;

			if ( repositionGameObject )
			{
				UpdateMyLinearMapping( transform );
			}
			
		}
        private void OnEnable()
        {
			startPosition.SetParent(transform.parent);
			endPosition.SetParent(transform.parent);
        }
        private void OnDisable()
        {
            //startPosition.SetParent(transform);
            //endPosition.SetParent(transform);
        }

        public void MyExitHand()
        {
            
            sampleCount = 0;
            mappingChangeRate = 0.0f;
          
        }
        public bool needTool = false;
        

        public virtual void HandHoverUpdate(SingleTransform hand)
        {
            if (needTool) return;
            GrabTypes startingGrabType = hand.startingGrabType;

            LinearMapping.player = hand.virtualTransform.root.name;
            if ( startingGrabType != GrabTypes.None)
            {
                UpdateMyLinearMapping(hand.virtualTransform);
            }
			
        }

       
		public void OnHandHoverEnd(SingleTransform singleTransform)
		{
            initialMappingOffset = LinearMapping.Value - CalculateMyLinearMapping(singleTransform.virtualTransform);
            sampleCount = 0;
            mappingChangeRate = 0.0f;
        }

        
        protected virtual void HandAttachedUpdate(SingleTransform hand)
        {
            UpdateMyLinearMapping(hand.virtualTransform);

        }

       



        protected void CalculateMappingChangeRate()
		{
			//Compute the mapping change rate
			mappingChangeRate = 0.0f;
			int mappingSamplesCount = Mathf.Min( sampleCount, mappingChangeSamples.Length );
			if ( mappingSamplesCount != 0 )
			{
				for ( int i = 0; i < mappingSamplesCount; ++i )
				{
					mappingChangeRate += mappingChangeSamples[i];
				}
				mappingChangeRate /= mappingSamplesCount;
			}
		}

        protected void UpdateMyLinearMapping( Transform updateTransform )
		{
			prevMapping = LinearMapping.Value;
			LinearMapping.Value = Mathf.Clamp01( initialMappingOffset + CalculateMyLinearMapping( updateTransform ) );

			mappingChangeSamples[sampleCount % mappingChangeSamples.Length] = ( 1.0f / Time.deltaTime ) * ( LinearMapping.Value - prevMapping );
			sampleCount++;

			if ( repositionGameObject )
			{
				transform.position = Vector3.Lerp( startPosition.position, endPosition.position, LinearMapping.Value );
			}
		}

        protected float CalculateMyLinearMapping( Transform updateTransform )
		{
			Vector3 direction = endPosition.position - startPosition.position;
			float length = direction.magnitude;
			direction.Normalize();

			Vector3 displacement = updateTransform.position - startPosition.position;

			return Vector3.Dot( displacement, direction ) / length;
		}
        


        protected virtual void Update()
        {
            if ( maintainMomemntum && mappingChangeRate != 0.0f )
			{
				//Dampen the mapping change rate and apply it to the mapping
				mappingChangeRate = Mathf.Lerp( mappingChangeRate, 0.0f, momemtumDampenRate * Time.deltaTime );
				LinearMapping.Value = Mathf.Clamp01( LinearMapping.Value + ( mappingChangeRate * Time.deltaTime ) );

				if ( repositionGameObject )
				{
					transform.position = Vector3.Lerp( startPosition.position, endPosition.position, LinearMapping.Value );
				}
			}
		}
	}

