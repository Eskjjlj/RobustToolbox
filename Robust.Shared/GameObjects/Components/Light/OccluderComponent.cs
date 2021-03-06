using System;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    public class OccluderComponent : Component
    {
        public sealed override string Name => "Occluder";
        public sealed override uint? NetID => NetIDs.OCCLUDER;

        private bool _enabled = true;
        private Box2 _boundingBox = new(-0.5f, -0.5f, 0.5f, 0.5f);

        [ViewVariables(VVAccess.ReadWrite)]
        public Box2 BoundingBox
        {
            get => _boundingBox;
            set
            {
                _boundingBox = value;
                Dirty();
                BoundingBoxChanged();
            }
        }

        private void BoundingBoxChanged()
        {
            Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local, new OccluderBoundingBoxChangedMessage(this));
        }

        [ViewVariables(VVAccess.ReadWrite)]
        public virtual bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value)
                    return;

                _enabled = value;
                Dirty();
            }
        }

        protected override void Startup()
        {
            base.Startup();

            EntitySystem.Get<OccluderSystem>().AddOrUpdateEntity(Owner, Owner.Transform.Coordinates);
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _enabled, "enabled", true);
            serializer.DataField(ref _boundingBox, "boundingBox", new Box2(-0.5f, -0.5f, 0.5f, 0.5f));
        }

        public override void OnRemove()
        {
            base.OnRemove();

            var transform = Owner.Transform;
            var map = transform.MapID;
            if (map != MapId.Nullspace)
            {
                Owner.EntityManager.EventBus.RaiseEvent(EventSource.Local,
                    new OccluderTreeRemoveOccluderMessage(this, map, transform.GridID));
            }
        }

        public override ComponentState GetComponentState(ICommonSession player)
        {
            return new OccluderComponentState(Enabled, BoundingBox);
        }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            if (curState == null)
            {
                return;
            }

            var cast = (OccluderComponentState) curState;

            Enabled = cast.Enabled;
            BoundingBox = cast.BoundingBox;
        }

        [NetSerializable, Serializable]
        private sealed class OccluderComponentState : ComponentState
        {
            public bool Enabled { get; }
            public Box2 BoundingBox { get; }

            public OccluderComponentState(bool enabled, Box2 boundingBox) : base(NetIDs.OCCLUDER)
            {
                Enabled = enabled;
                BoundingBox = boundingBox;
            }
        }
    }

    internal struct OccluderBoundingBoxChangedMessage
    {
        public OccluderComponent Occluder;

        public OccluderBoundingBoxChangedMessage(OccluderComponent occluder)
        {
            Occluder = occluder;
        }
    }
}
