using System.Collections.Generic;

namespace QuickUndoRedo.Core
{
    public class Undo
    {
        public event System.Action onUndoRedoPerformed;

        List<UndoSnapshot> undoPool;
        List<UndoSnapshot> redoPool;
        
        List<UndoSnapshot> undoStack;
        List<UndoSnapshot> redoStack;

        int undoIndex, redoIndex;

        IUndoFactory factory;

        public void Record()
        {
            if (undoIndex >= undoPool.Count)
                undoIndex = 0;
            var undoSnapshot = undoPool[undoIndex++];
            undoSnapshot.Clear();
            foreach (var u in factory.GetAll())
            {
                if (u.isDirty)
                {
                    undoSnapshot.Record(u);
                    u.isDirty = false;
                }
                else if (u.isJustCreated)
                {
                    undoSnapshot.RecordCreated(u);
                    u.isJustCreated = false;
                }
            }
            undoStack.Add(undoSnapshot);
        }

        public void PerformUndo()
        {
            if (undoStack.Count == 0)
                return;
            var snapshot = undoStack[undoStack.Count - 1];
            if (redoIndex >= redoPool.Count)
                redoIndex = 0;


            var redoSnapshot = redoPool[redoIndex++];
            redoSnapshot.Clear();
            foreach (var s in snapshot.states)
            {
                redoSnapshot.Record(s.Value);
            }
            foreach (var b in snapshot.createdInstances)
            {
                redoSnapshot.Record(b.Value);
            }
            redoStack.Add(redoSnapshot);
            if (redoStack.Count > redoPool.Count)
                redoStack.RemoveAt(0);

         
            snapshot.Load();
            undoStack.Remove(snapshot);
            if (onUndoRedoPerformed != null)
                onUndoRedoPerformed();
        }

        public void PerformRedo()
        {
            if (redoStack.Count == 0)
                return;

            var snapshot = redoStack[redoStack.Count - 1];
            if (undoIndex >= undoPool.Count)
                undoIndex = 0;
            var undoSnapshot = undoPool[undoIndex++];
            undoSnapshot.Clear();
            foreach (var s in snapshot.states)
            {
                undoSnapshot.Record(s.Value);
            }
            foreach (var b in snapshot.createdInstances)
            {
                undoSnapshot.Record(b.Value);
            }
            undoStack.Add(undoSnapshot);
            if (undoStack.Count > undoPool.Count)
                undoStack.RemoveAt(0);

            snapshot.Load();
            redoStack.RemoveAt(redoStack.Count - 1);
            if (onUndoRedoPerformed != null)
                onUndoRedoPerformed();
        }

        public Undo(IUndoFactory factory, int capacity, int undoablesCount)
        {
            this.factory = factory;
            undoPool = new List<UndoSnapshot>(capacity);
            redoPool = new List<UndoSnapshot>(capacity);
            undoStack = new List<UndoSnapshot>(capacity);
            redoStack = new List<UndoSnapshot>(capacity);
            for (int i = 0; i < capacity; i++)
            {
                undoPool.Add(new UndoSnapshot(undoablesCount, factory));
                redoPool.Add(new UndoSnapshot(undoablesCount, factory));
            }
        }

        class UndoSnapshot
        {
            public Dictionary<IUndoableState, IUndoable> states;

            public Dictionary<IUndoableState, IUndoable> createdInstances;

            IUndoFactory factory;

            public UndoSnapshot(int capacity, IUndoFactory factory)
            {
                this.factory = factory;
                states = new Dictionary<IUndoableState, IUndoable>(capacity);
                createdInstances = new Dictionary<IUndoableState, IUndoable>(capacity);
            }

            public void Clear()
            {
                createdInstances.Clear();
                states.Clear();
            }

            public void Record(IUndoable undoable)
            {
                states.Add(undoable.SaveState(), undoable);
            }


            public void RecordCreated(IUndoable b)
            {
                createdInstances.Add(b.SaveState(), b);
            }

            public void Load()
            {
                foreach (var s in states)
                {
                    var state = s.Key;
                    if (factory.HasKey(state.ID))
                    {
                        s.Value.LoadState(state);
                    }
                    else
                    {
                        var inst = factory.CreateWithID(state.sourcePath, state.ID);
                        inst.LoadState(state);
                    }
                }
                foreach (var b in createdInstances)
                {
                    factory.Delete(b.Value);
                }
            }
        }
    }


}


