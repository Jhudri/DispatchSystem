﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

using Config.Reader;

using DispatchSystem.Common.DataHolders.Storage;

using CloNET;
using CloNET.Callbacks;
using CloNET.LocalCallbacks;

using CitizenFX.Core;
using DispatchSystem.Common;

namespace DispatchSystem.sv.External
{
    public class DispatchServer
    {
        private Server server; // server from CloNET
        private readonly string ip; // ip of the server
        private readonly int port; // port of the server

        /// <summary>
        /// <see cref="Array"/> of <see cref="ConnectedPeer"/> that are currently connected to the server
        /// </summary>
        public ConnectedPeer[] ConnectedDispatchers => server.ConnectedPeers.Where(x => x.IsConnected).ToArray();

        // 911calls items, for keeping track of dispatchers connected to which 911 call
        internal Dictionary<Guid, string> Calls;

        /// <summary>
        /// Initializes the <see cref="DispatchSystem"/> class from a config
        /// </summary>
        /// <param name="cfg"></param>
        internal DispatchServer(ServerConfig cfg)
        {
            Calls = new Dictionary<Guid, string>(); // creating the calls item
            ip = cfg.GetStringValue("server", "ip", "0.0.0.0"); // setting the ip
            Log.WriteLine("Setting ip to " + ip);
            port = cfg.GetIntValue("server", "port", 33333); // setting the port
            Log.WriteLine("Setting port to " + port);
            Start(); // starting the server
        }

        private void Start()
        {
            Log.WriteLine("Creating TCP Device"); // moar logs
            server = new Server(ip, port) // creating the server from the port ant ip
            {
                Encryption = new EncryptionOptions // setting encryption off
                {
                    Encrypt = false,
                    Overridable = false
                }
            };

            // server events
            server.Connected += OnConnect;
            server.Disconnected += OnDisconnect;
            Log.WriteLine("TCP Created, starting TCP");
            // starting the server
            try { server.Listening = true; }
            // catching a port in use exception
            catch (SocketException)
            {
                Log.WriteLine("The specified port (" + port + ") is already in use.");
                return;
            }
            Log.WriteLine("TCP Started, Listening for connections...");

            AddCallbacks(); // adding the callbacks to the server for use from client
        }

        private void AddCallbacks()
        {
            // funcs that return objects from params given
            server.LocalCallbacks.Functions = new MemberDictionary<string, LocalFunction>
            {
                {"GetCivilian", new LocalFunction(GetCivilian)},
                {"GetCivilianVeh", new LocalFunction(GetCivilianVeh)},
                {"GetOfficer", new LocalFunction(GetOfficer)},
                {"CreateAssignment", new LocalFunction(NewAssignment)},
                {"GetOfficerAssignment", new LocalFunction(GetOfcAssignment)},
                {"Accept911", new LocalFunction(AcceptEmergency)}
            };
            // properties that only have a return, and no set
            server.LocalCallbacks.Properties = new MemberDictionary<string, LocalProperty>
            {
                {"Bolos", new LocalProperty(GetBolos, null)},
                {"Officers", new LocalProperty(GetOfficers, null)},
                {"Assignments", new LocalProperty(GetAssignments, null)}
            };
            // events that have params but no return
            server.LocalCallbacks.Events = new MemberDictionary<string, LocalEvent>
            {
                {"911End", new LocalEvent(EndEmergency)},
                {"911Msg", new LocalEvent(MessageEmergency)},
                {"AddOfficerAssignment", new LocalEvent(AddOfcAssignment)},
                {"RemoveAssignment", new LocalEvent(RemoveAssignment)},
                {"RemoveOfficerAssignment", new LocalEvent(RemoveOfcAssignment)},
                {"SetStatus", new LocalEvent(ChangeOfficerStatus)},
                {"RemoveOfficer", new LocalEvent(RemoveOfficer)},
                {"AddBolo", new LocalEvent(AddBolo)},
                {"RemoveBolo", new LocalEvent(RemoveBolo)},
                {"AddNote", new LocalEvent(AddNote)}
            };
        }

        private static async Task OnConnect(ConnectedPeer user)
        {
            await Task.Run(delegate
            {
                // logging the ip connected
#if DEBUG
                Log.WriteLine($"[{user.RemoteIP}] Connected");
#else
                Log.WriteLineSilent($"[{user.RemoteIP}] Connected");
#endif

                // dispose if the permissions bad
                if (_(user))
                    user.Dispose();
            });
        }

        private static async Task OnDisconnect(ConnectedPeer user)
        {
            await Task.Run(delegate
            {
                // logging the ip disconnected
#if DEBUG
                Log.WriteLine($"[{user.RemoteIP}] Disconnected");
#else
                Log.WriteLineSilent($"[{user.RemoteIP}] Disconnected");
#endif
            });
        }

        /*
           _____              _        _        ____                _____   _  __   _____     
          / ____|     /\     | |      | |      |  _ \      /\      / ____| | |/ /  / ____|  _ 
         | |         /  \    | |      | |      | |_) |    /  \    | |      | ' /  | (___   (_)
         | |        / /\ \   | |      | |      |  _ <    / /\ \   | |      |  <    \___ \     
         | |____   / ____ \  | |____  | |____  | |_) |  / ____ \  | |____  | . \   ____) |  _ 
          \_____| /_/    \_\ |______| |______| |____/  /_/    \_\  \_____| |_|\_\ |_____/  (_)

        */

        private async Task<object> GetCivilian(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Get civilian Request Recieved");
#else
            Log.WriteLineSilent("[{sender.RemoteIP}] Get civilian Request Recieved");
#endif

            // getting vars from args
            string first = args[0];
            string last = args[1];

            // tryna find civ using common
            Civilian civ = Common.GetCivilianByName(first, last);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Sending Civilian information to Client");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Sending Civilian information to Client");
#endif
            return civ;
        }
        private async Task<object> GetCivilianVeh(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Get civilian veh Request Recieved");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Get civilian veh Request Recieved");
#endif

            // var from the given params
            string plate = args[0];

            // tryna find the vehicle
            CivilianVeh civVeh = Common.GetCivilianVehByPlate(plate);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Sending Civilian Veh information to Client");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Sending Civilian Veh information to Client");
#endif
            return civVeh;
        }
        private async Task<object> GetOfficer(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);

            // getting the id from the params
            Guid id = args[0];

#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Get officer Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Get officer Request Received");
#endif

            // finding the officer from the list
            Officer ofc = DispatchSystem.Officers.ToList().Find(x => x.Id == id);
            return ofc;
        }
        private async Task<object> GetOfcAssignment(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Get officer assignments Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Get officer assignments Request Received");
#endif
            // getting id from params
            Guid id = args[0];
            // finding the officer in the list
            Officer ofc = DispatchSystem.Officers.ToList().Find(x => x.Id == id);

            // finding assignment in common
            return Common.GetOfficerAssignment(ofc);
        }
        private async Task<object> AcceptEmergency(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Accept emergency Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Accept emergency Request Received");
#endif

            // id from params
            Guid id = args[0];
            // finding the call in the current calls
            EmergencyCall acceptedCall = DispatchSystem.CurrentCalls.FirstOrDefault(x => x.Id == id);
            if (Calls.ContainsKey(id) || acceptedCall == null) return false; // Checking null and accepted in same expression

            Calls.Add(id, sender.RemoteIP); // adding the call and dispatcher to the call list
            // setting a message for invocation on the main thread
            DispatchSystem.Invoke(delegate
            {
                Player p = Common.GetPlayerByIp(acceptedCall.SourceIP);
                if (p != null)
                {
                    Common.SendMessage(p, "Dispatch911", new [] {255,0,0}, "Your 911 call has been accepted by a Dispatcher");
                }
            });
            return true;
        }
        private async Task<object> GetBolos(ConnectedPeer sender)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Get bolos Request Recieved");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Get bolos Request Recieved");
#endif

            return DispatchSystem.ActiveBolos;
        }
        private async Task<object> GetOfficers(ConnectedPeer sender)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Get officers Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Get officers Request Received");
#endif

            return DispatchSystem.Officers;
        }
        private async Task<object> GetAssignments(ConnectedPeer sender)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Get assignments Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Get assignments Request Received");
#endif

            return DispatchSystem.Assignments;
        }
        private async Task EndEmergency(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] End emergency Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] End emergency Request Received");
#endif

            // getting the id from the params
            Guid id = args[0];
            Calls.Remove(id); // removing the id from the calls

            EmergencyCall call = DispatchSystem.CurrentCalls.FirstOrDefault(x => x.Id == id); // obtaining the call from the civ

            if (DispatchSystem.CurrentCalls.Remove(call)) // remove, if successful, then notify
            {
                DispatchSystem.Invoke(delegate
                {
                    Player p = Common.GetPlayerByIp(call?.SourceIP);

                    if (p != null)
                    {
                        Common.SendMessage(p, "Dispatch911", new [] {255,0,0}, "Your 911 call was ended by a Dispatcher");
                    }
                });
            }
        }
        private async Task MessageEmergency(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Message emergency Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Message emergency Request Received");
#endif

            // getting items from params
            Guid id = args[0];
            string msg = args[1];

            EmergencyCall call = DispatchSystem.CurrentCalls.FirstOrDefault(x => x.Id == id); // finding the call

            DispatchSystem.Invoke(() =>
            {
                Player p = Common.GetPlayerByIp(call?.SourceIP); // getting the player from the call's ip

                if (p != null)
                {
                    Common.SendMessage(p, "Dispatcher", new [] {0x0,0xff,0x0}, msg);
                }
            });
        }
        private async Task AddOfcAssignment(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Add officer assignment Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Add officer assignment Request Received");
#endif

            // getting items from params
            Guid id = args[0];
            Guid ofcId = args[1];

            Assignment assignment = DispatchSystem.Assignments.Find(x => x.Id == id); // finding assignment from the id
            Officer ofc = DispatchSystem.Officers.ToList().Find(x => x.Id == ofcId); // finding the officer from the id
            if (assignment is null || ofc is null) // returning if either is null
                return;
            if (DispatchSystem.OfcAssignments.ContainsKey(ofc)) // returning if the officer already contains the assignment
                return;

            DispatchSystem.OfcAssignments.Add(ofc, assignment); // adding the assignment to the officer

            ofc.Status = OfficerStatus.OffDuty;

            // notify of assignment
            DispatchSystem.Invoke(() =>
            {
                Player p = Common.GetPlayerByIp(ofc.SourceIP);
                if (p != null)
                    Common.SendMessage(p, "^8DispatchCAD", new[] { 0, 0, 0 }, $"New assignment added: \"{assignment.Summary}\"");
            });
        } 
        private async Task<object> NewAssignment(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] New assignment Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] New assignment Request Received");
#endif

            // getting summary from params
            string summary = args[0];

            Assignment assignment = new Assignment(summary);
            DispatchSystem.Assignments.Add(assignment);
            return assignment.Id; // returning the assingment id
        }
        private async Task RemoveAssignment(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Remove assignment Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Remove assignment Request Received");
#endif

            // getting the id from the params
            Guid item = args[0];

            Assignment item2 = DispatchSystem.Assignments.Find(x => x.Id == item); // finding the assignment from the id
            Common.RemoveAllInstancesOfAssignment(item2); // removing using common
        }
        private async Task RemoveOfcAssignment(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Remove officer assignment Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Remove officer assignment Request Received");
#endif

            // getting id from params
            Guid ofcId = args[0];

            // finding the ofc
            Officer ofc = DispatchSystem.Officers.FirstOrDefault(x => x.Id == ofcId);
            if (ofc == null) return;

            if (!DispatchSystem.OfcAssignments.ContainsKey(ofc)) return;
            DispatchSystem.OfcAssignments.Remove(ofc); // removing the assignment from the officer

            ofc.Status = OfficerStatus.OnDuty; // set on duty

            DispatchSystem.Invoke(() =>
            {
                Player p = Common.GetPlayerByIp(ofc.SourceIP);
                if (p != null)
                    Common.SendMessage(p, "^8DispatchCAD", new[] { 0, 0, 0 }, "Your assignment has been removed by a dispatcher");
            });
        }
        private async Task ChangeOfficerStatus(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Change officer status Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Change officer status Request Received");
#endif

            // getting items from params
            Guid id = args[0];
            OfficerStatus status = args[1];

            // finding the officer
            Officer ofc = DispatchSystem.Officers.FirstOrDefault(x => x.Id == id);

            if (ofc is null) return; // checking for null

            if (ofc.Status != status)
            {
                ofc.Status = status; // changing the status
#if DEBUG
                Log.WriteLine($"[{sender.RemoteIP}] Setting officer status to " + status);
#else
                Log.WriteLineSilent($"[{sender.RemoteIP}] Setting officer status to " + status);
#endif

                DispatchSystem.Invoke(() =>
                {
                    Player p = Common.GetPlayerByIp(ofc.SourceIP);
                    if (p != null)
                        Common.SendMessage(p, "^8DispatchCAD", new[] { 0, 0, 0 },
                            $"Dispatcher set status to {(ofc.Status == OfficerStatus.OffDuty ? "Off Duty" : ofc.Status == OfficerStatus.OnDuty ? "On Duty" : "Busy")}");
                });
            }
            else
            {
#if DEBUG
                Log.WriteLine($"[{sender.RemoteIP}] Officer status already set to the incoming status");
#else
                Log.WriteLineSilent($"[{sender.RemoteIP}] Officer status already set to the incoming status");
#endif
            }
        }
        private async Task RemoveOfficer(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Remove officer Request Received");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Add bolo Request Received");
#endif

            // getting items from given params
            Guid ofcGiven = args[0];

            // getting the officer
            Officer ofc = DispatchSystem.Officers.FirstOrDefault(x => x.Id == ofcGiven);
            if (ofc != null)
            {
                // notify of removing of role
                DispatchSystem.Invoke(delegate
                {
                    Player p = Common.GetPlayerByIp(ofc.SourceIP);

                    if (p != null)
                        Common.SendMessage(p, "^8DispatchCAD", new[] { 0, 0, 0 }, "You have been removed from your officer role by a dispatcher");
                });

                // actually remove the officer from the list
                DispatchSystem.Officers.Remove(ofc);

#if DEBUG
                Log.WriteLine($"[{sender.RemoteIP}] Removed the officer from the list of officers");
#else
                Log.WriteLineSilent($"[{sender.RemoteIP}] Removed the officer from the list of officers");
#endif
            }
            else
            {
#if DEBUG
                Log.WriteLine($"[{sender.RemoteIP}] Officer in list not found, not removing");
#else
                Log.WriteLineSilent($"[{sender.RemoteIP}] Officer in list not found, not removing");
#endif
            }
        }
        private async Task AddBolo(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Add bolo Request Recieved");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Add bolo Request Recieved");
#endif

            // getting the items from the params
            string player = args[0];
            string bolo = args[1];

#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Adding new Bolo for \"{bolo}\"");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Adding new Bolo for \"{bolo}\"");
#endif
            // adding bolo
            DispatchSystem.ActiveBolos.Add(new Bolo(player, string.Empty, bolo));
        }
        private async Task RemoveBolo(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Remove bolo Request Recieved");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Remove bolo Request Recieved");
#endif

            // getting the index from the given
            int parse = args[0];

            try
            {
                // removing at the specified index
                DispatchSystem.ActiveBolos.RemoveAt(parse);
#if DEBUG
                Log.WriteLine($"[{sender.RemoteIP}] Removed Active BOLO from the List");
#else
                Log.WriteLineSilent($"[{sender.RemoteIP}] Removed Active BOLO from the List");
#endif
            }
            // thrown when argument is out of range
            catch (ArgumentOutOfRangeException)
            {
#if DEBUG
                Log.WriteLine($"[{sender.RemoteIP}] Index for BOLO not found, not removing...");
#else
                Log.WriteLineSilent($"[{sender.RemoteIP}] Index for BOLO not found, not removing...");
#endif
            }
        }
        private async Task AddNote(ConnectedPeer sender, dynamic[] args)
        {
            await Task.FromResult(0);
#if DEBUG
            Log.WriteLine($"[{sender.RemoteIP}] Add Civilian note Request Recieved");
#else
            Log.WriteLineSilent($"[{sender.RemoteIP}] Add Civilian note Request Recieved");
#endif

            // getting the params from the given
            Guid id = args[0];
            string note = args[1];

            Civilian civ = DispatchSystem.Civs.FirstOrDefault(x => x.Id == id); // finding the civ from the id

            if (civ != null)
            {
#if DEBUG
                Log.WriteLine($"[{sender.RemoteIP}] Adding the note \"{note}\" to Civilian {civ.First} {civ.Last}");
#else
                Log.WriteLineSilent($"[{sender.RemoteIP}] Adding the note \"{note}\" to Civilian {civ.First} {civ.Last}");
#endif
                civ.Notes.Add(note); // adding the note for the civilian
            }
            else
#if DEBUG
                Log.WriteLine($"[{sender.RemoteIP}] Civilian not found, not adding note...");
#else
                Log.WriteLineSilent($"[{sender.RemoteIP}] Civilian not found, not adding note...");
#endif
        }

        private static bool _(ConnectedPeer sender)
        {
            switch (Permissions.Get.DispatchPermission)
            {
                case Permission.Specific: // checking for specific permissions
                    if (!Permissions.Get.DispatchContains(IPAddress.Parse(sender.RemoteIP))) // checking if the ip is in the perms
                    {
                        Log.WriteLine($"[{sender.RemoteIP}] NOT ENOUGH DISPATCH PERMISSIONS"); // log if not
                        return true;
                    }
                    break;
                case Permission.None:
                    Log.WriteLine($"[{sender.RemoteIP}] NOT ENOUGH DISPATCH PERMISSIONS");
                    return true; // automatically return not
                case Permission.Everyone:
                    break; // continue through
                default:
                    throw new ArgumentOutOfRangeException(); // throw because there is no other options
            }
            return false; // return false by default
        }
    }
}
