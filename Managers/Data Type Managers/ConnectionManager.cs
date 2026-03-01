using Rendering.UI;

namespace Managers
{
    /// <summary>
    /// <para>Manages connections and ConnectionUIs</para>
    /// </summary>

    public static class ConnectionManager
    {
        private static HashSet<Connection> connections = null!;
        public static HashSet<Connection> GetAllConnections => connections;
        public static HashSet<ConnectionUI> ActiveConnections = null!;
        private static Guid CurrentParentEntry;
        private static EntryUI? Source = null;

        public static void AddConnections(List<Connection> targetConnections)
        {
            targetConnections.Where(x => x != null).ToList().ForEach(x => connections.Add(x));
        }

        public static void Init()
        {
            connections = [];
            ActiveConnections = [];
        }

        public static void StartConnection(UIObject? uIObject)
        {
            if (uIObject is EntryUI entryUI)
            {
                Source = entryUI;
            }
            else
            {
                Source = null;
            }
        }

        public static void EndConnection(UIObject? uIObject)
        {
            if (uIObject is EntryUI entryUI && Source != null)
            {
                CreateNewConnection(Source.ReferenceEntry, entryUI.ReferenceEntry);
            }

            Source = null;
        }

        public static void EndConnectionFromAllSelected(UIObject? uIObject)
        {
            if (uIObject is EntryUI entryUI)
            {
                var sources = SelectionManager.GetSelectedTypeOfObject<EntryUI>();
                List<Guid> CreatedConnections = [];
                foreach (var targetUi in sources)
                {
                    var createdConnection = CreateNewConnection(targetUi.ReferenceEntry, entryUI.ReferenceEntry);
                    if (createdConnection != null)
                    {
                        CreatedConnections.Add(createdConnection.guid);
                    }
                }

                var action = new UndoRedoAction(
                    undoActions: [() => UnmarkConnectionsAsDeleted(CreatedConnections)],
                    redoActions: [() => MarkConnectionsAsDeleted(CreatedConnections)]
                );

                UndoRedoManager.ActionExecuted(action);
                UpdateAllConnections();
            }

            // just in case
            Source = null;
        }

        public static void LoadFromSave(List<Connection> TargetConnections)
        {
            connections = [.. TargetConnections];
        }

        internal static void LoadConnections(Guid? target)
        {
            if (target == null) { return; }

            ActiveConnections.Clear();

            CurrentParentEntry = (Guid)target;

            List<Connection> targetConnections = [.. connections.Where(x => x.ParentEntryGuid == target)];
            CreateNewConnectionUIs(targetConnections);
        }

        public static Connection? CreateNewConnection(Entry source, Entry target, ArrowType connectionType = ArrowType.Default, bool createAction = true)
        {
            if (source.guid == target.guid) { return null; }

            /* var existingConnection = connections.FirstOrDefault(x => (x.SourceEntry == source.guid && x.TargetEntry == target.guid) ||
                 (x.TargetEntry == source.guid && x.SourceEntry == target.guid));*/

            var existingConnection = connections.FirstOrDefault(x => x.SourceEntry == source.guid && x.TargetEntry == target.guid);

            if (existingConnection != null && existingConnection != default)
            {
                if (existingConnection.IsDeleted)
                {
                    existingConnection.arrowType = ArrowType.Default;
                    UnmarkConnectionsAsDeleted([existingConnection.guid]);
                }
                return null;
            }

            Connection connection = new(CurrentParentEntry, source.guid, target.guid)
            {
                arrowType = connectionType
            };
            CreateNewConnectionUI(connection)?.UpdateConnection();
            connections.Add(connection);
            SelectionManager.ClearSelection();

            if (createAction)
            {
                var action = new UndoRedoAction(
                undoActions: [() => MarkConnectionsAsDeleted([connection.guid])],
                redoActions: [() => UnmarkConnectionsAsDeleted([connection.guid])]
                );

                UndoRedoManager.ActionExecuted(action);
            }

            AudioHandler.PlaySound("pluck_001");

            return connection;
        }

        public static void UpdateConnectionsForEntry(Guid entryGuid)
        {
            var affectedConnections = ActiveConnections.Where(x =>
                x.ReferenceConnection.SourceEntry == entryGuid ||
                x.ReferenceConnection.TargetEntry == entryGuid);

            foreach (var connectionUI in affectedConnections)
            {
                connectionUI.UpdateConnection();
            }
        }

        public static List<ConnectionUI> CreateNewConnectionUIs(List<Connection> conns)
        {
            List<ConnectionUI> CreatedUIs = [];
            foreach (Connection con in conns)
            {
                var createdConn = CreateNewConnectionUI(con);
                if (createdConn != null)
                {
                    CreatedUIs.Add(createdConn);
                }
            }
            UpdateAllConnections();
            return CreatedUIs;
        }

        public static ConnectionUI? CreateNewConnectionUI(Connection conn)
        {
            try
            {
                ConnectionUI uI = new(conn)
                {
                    RenderOrder = 2,
                    IsDraggable = false,
                };

                ChunkManager.AddObject(uI);
                ActiveConnections.Add(uI);
                return uI;
            }
            catch (Exception ex)
            {
                Logger.Log("ConnectionManager", $"Error creating ConnectionUI: {ex.Message}\n Stacktrace:\n{ex.StackTrace}", LogLevel.FATAL);
                return null;
            }
        }

        public static void MarkSelectedConnectionsAsDeleted()
        {
            List<ConnectionUI>? affected = SelectionManager.GetSelectedTypeOfObject<ConnectionUI>();
            if (affected == null || affected.Count < 1) { return; }

            List<Guid> guids = [.. affected.Select(x => x.ReferenceConnection.guid)];
            MarkConnectionsAsDeleted(guids);

            var action = new UndoRedoAction(
                undoActions: [() => UnmarkConnectionsAsDeleted(guids)],
                redoActions: [() => MarkConnectionsAsDeleted(guids)],
                PopActions: [() => RemoveConnections(guids)]
            );

            UndoRedoManager.ActionExecuted(action);
        }

        public static void SetArrowType(List<ConnectionUI>? uIs, ArrowType target)
        {
            if (uIs == null || uIs.Count < 1) { return; }
            foreach (var ui in uIs)
            {
                if (ui.ReferenceConnection == null) continue;
                ui.ReferenceConnection.arrowType = target;
                ui.UpdateConnection();
            }
        }

        public static List<Connection> GetConnectionsForEntry(Guid entryGuid)
        {
            return [.. connections.Where(x =>
                x.SourceEntry == entryGuid ||
                x.TargetEntry == entryGuid)];
        }

        public static void UpdateAllConnections()
        {
            foreach (var con in ActiveConnections)
            {
                con.UpdateConnection();
            }
        }

        public static void Dispose()
        {
            foreach (var con in ActiveConnections)
            {
                con.Dispose();
            }
        }

        public static void MarkConnectionsAsDeleted(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                var con = connections.FirstOrDefault(x => x.guid == guid);
                if (con != default && con != null)
                {
                    con.IsDeleted = true;

                    var conui = ActiveConnections.FirstOrDefault(x => x.ReferenceConnection == con);
                    if (conui != default && conui != null)
                    {
                        conui.IsVisible = false;
                        SelectionManager.Deselect(conui);
                    }
                }
            }
            UpdateAllConnections();
        }

        public static void UnmarkConnectionsAsDeleted(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                var con = connections.FirstOrDefault(x => x.guid == guid);
                if (con != default && con != null)
                {
                    con.IsDeleted = false;

                    var conui = ActiveConnections.FirstOrDefault(x => x.ReferenceConnection == con);
                    if (conui != default && conui != null)
                    {
                        conui.IsVisible = true;
                    }
                }
            }
            UpdateAllConnections();
        }

        public static void CleanUpDeletedConnections()
        {
            var entriesToRemove = connections.Where(x => x == null || x.IsDeleted).ToList();
            entriesToRemove.ForEach(x => connections.Remove(x));
        }


        public static void RemoveConnections(List<Guid> guids)
        {
            foreach (var guid in guids)
            {
                var con = connections.FirstOrDefault(x => x.guid == guid);
                if (con != default && con != null)
                {
                    connections.Remove(con);
                }
            }
        }

        public static void RecalcConnectionSizes()
        {
            foreach (ConnectionUI conn in ActiveConnections)
            {
                conn?.UpdateConnection();
            }
        }
    }
}