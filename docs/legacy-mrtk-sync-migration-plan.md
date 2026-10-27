# Legacy MRTK Sync Migration Plan

> Historical migration plan. Current legacy sync assets live under
> `Assets/APP/Modules/LegacyMrtkSync` and `Assets/APP/ScenesNGOsync`; paths such
> as `Assets/Modules/LegacyMrtkSync` and `Assets/_syc_resource` below describe
> earlier source/target states. Do not execute this plan without a fresh
> inventory. See [current-project-baseline.md](./current-project-baseline.md).

## Scope

- Source project: `D:\UnityProject\gitea_mrtk_sync_old`
- Target project: `D:\UnityProject\git_BPF`
- Architecture reference: `D:\UnityProject\gitea_bpf_sample-main\gitea_bpf_sample`
- Collaboration guide: `C:\Users\garfi\Downloads\bpf-frontend-collaboration-guide.md`

## Confirmed Constraints

- Keep BPF business configuration under `Assets/Resources/BusinessPrefabs` and `Assets/Resources/TopicPrefabs`.
- Do not hand-edit generated `Assets/StreamingAssets/Config.xml` as the source of truth.
- Preserve prefab / scene naming alignment for runtime loading.
- Treat legacy scene names such as `Init`, `Lobby`, `UDPLobby`, and `MRTKsync` as legacy-only unless explicitly being migrated.
- Put migrated legacy sync runtime under the current app module structure, not back under `_syc_resource`.

## Architecture Mapping

| Legacy area | Target location | Purpose |
| --- | --- | --- |
| `Assets/_syc_resource/_my_scripts` | `Assets/APP/Modules/LegacyMrtkSync/Scripts` | Runtime sync, lobby, UDP discovery, UI controllers |
| `Assets/_syc_resource/_my_scenes` | `Assets/APP/Modules/LegacyMrtkSync/Scenes` | Reference scenes for controlled extraction / prefab recreation |
| `Assets/_syc_resource/_my_resources/font` | `Assets/APP/Modules/LegacyMrtkSync/Resources/Fonts` | Fonts required by legacy UI references |
| Legacy networking scenes | Current BPF scenes / prefabs after validation | Integrate behavior without switching mainline flow |
| Legacy task/content prefabs | `Assets/Resources/BusinessPrefabs` / `TopicPrefabs` | Only after names and serialized references are verified |

## First Batch Copied

| Area | From | To | Files |
| --- | --- | --- | ---: |
| RuntimeScripts | `Assets\_syc_resource\_my_scripts` | `Assets\APP\Modules\LegacyMrtkSync\Scripts` | 22 source scripts + 1 bootstrap |
| LegacyScenes | `Assets\_syc_resource\_my_scenes` | `Assets\APP\Modules\LegacyMrtkSync\Scenes` | 8 Unity scenes + metas |
| LegacyFonts | `Assets\_syc_resource\_my_resources\font` | `Assets\APP\Modules\LegacyMrtkSync\Resources\Fonts` | 18 font/resource files + metas |

## Source Scenes Inventory

- `Assets\_syc_resource\_my_scenes\00Init\Init.unity`
- `Assets\_syc_resource\_my_scenes\02Lobby\Lobby.unity`
- `Assets\_syc_resource\_my_scenes\sync\MRTKsync.unity`
- `Assets\_syc_resource\_my_scenes\sync\MRTKsync1.unity`
- `Assets\_syc_resource\_my_scenes\sync\MRTKsync2.unity`
- `Assets\_syc_resource\_my_scenes\sync\MRTKsyncrigbroken.unity`
- `Assets\_syc_resource\_my_scenes\sync\MRTKsyncsuccess.unity`
- `Assets\_syc_resource\_my_scenes\T04UDPSprate\UDPLobby.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveLevels\Scenes\MirrorAdditiveLevelsOffline.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveLevels\Scenes\MirrorAdditiveLevelsOnline.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveLevels\Scenes\MirrorAdditiveLevelsSubLevel1.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveLevels\Scenes\MirrorAdditiveLevelsSubLevel2.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveLevels\Scenes\Offline.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveLevels\Scenes\Online.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveLevels\Scenes\SubLevel1.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveLevels\Scenes\SubLevel2.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveScenes\Scenes\MainScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveScenes\Scenes\MirrorAdditiveScenesMain.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveScenes\Scenes\MirrorAdditiveScenesSubScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\AdditiveScenes\Scenes\SubScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Basic\Scenes\Example.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Basic\Scenes\MirrorBasic.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Benchmark\Scenes\MirrorBenchmark.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Benchmark\Scenes\Scene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\BenchmarkIdle\BenchmarkIdle.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\BenchmarkIdle\MirrorBenchmarkIdle.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\BenchmarkStinkySteak\Main.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Billiards\MirrorBilliards.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\BilliardsPredicted\MirrorBilliardsPredicted.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\CCU\CCU.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\CCU\MirrorCCU.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\CharacterSelection\MirrorCharacterSelection.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\CharacterSelection\MirrorCharacterSelectionNoCharacter.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\CharacterSelection\MirrorCharacterSelectionPreScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Chat\Scenes\Main.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Chat\Scenes\MirrorChat.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\CouchCoop\MirrorCouchCoop.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Discovery\Scenes\MirrorDiscovery.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Discovery\Scenes\Scene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\LagCompensation\LagCompensation.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\LagCompensation\MirrorLagCompensation.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\MultipleAdditiveScenes\Scenes\Game.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\MultipleAdditiveScenes\Scenes\Main.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\MultipleAdditiveScenes\Scenes\MirrorMultipleAdditiveScenesGame.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\MultipleAdditiveScenes\Scenes\MirrorMultipleAdditiveScenesMain.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\MultipleMatches\Scenes\Main.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\MultipleMatches\Scenes\MirrorMultipleMatches.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Pong\Scenes\MirrorPong.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Pong\Scenes\Scene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\RigidbodyBenchmark\Scenes\MirrorRigidbodyBenchmark.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\RigidbodyBenchmark\Scenes\RigidbodyBenchmark.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\RigidbodyPhysics\Scenes\BounceScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\RigidbodyPhysics\Scenes\MirrorBounceScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Room\Scenes\MirrorRoomGame.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Room\Scenes\MirrorRoomOffline.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Room\Scenes\MirrorRoomOnline.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Room\Scenes\OfflineScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Room\Scenes\OnlineScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Room\Scenes\RoomScene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Snapshot Interpolation\MirrorSnapshotInterpolation.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Snapshot Interpolation\SnapshotInterpolation.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\SyncDirection\MirrorSyncDirection.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\SyncDirection\Scene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Tanks\Scenes\MirrorTanks.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\Tanks\Scenes\Scene.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\TanksCoop\SceneTanksCoop.unity`
- `Assets\GameMain\Plugins\Mirror\Examples\TankTheftAuto\MirrorTankTheftAuto.unity`
- `Assets\GameMain\Plugins\QuickOutline\Samples\Scenes\QuickOutline.unity`
- `Assets\GameMain\Scenes\202412\MRTK.unity`
- `Assets\GameMain\Scenes\202412\MultMRTK.unity`
- `Assets\GameMain\Scenes\202412\SyncSever.unity`
- `Assets\GameMain\Scenes\Conformity1.1AndTest\Conformity1.1 1.unity`
- `Assets\GameMain\Scenes\Conformity1.1AndTest\Conformity1.1.unity`
- `Assets\GameMain\Scenes\Conformity1.1AndTest\SampleScene.unity`
- `Assets\GameMain\Scenes\Conformity1.1AndTest\SceneCaptureDemo.unity`
- `Assets\GameMain\Scenes\Conformity1.1AndTest\ServiceTest.unity`
- `Assets\GameMain\Scenes\Conformity1.1AndTest\StandaloneDemo.unity`
- `Assets\GameMain\Scenes\Conformity1.1AndTest\Test.unity`
- `Assets\GameMain\Scenes\Conformity1.1AndTest\VideoChatDemo.unity`
- `Assets\GameMain\Scenes\Conformity2.1\FuWuQi.unity`

## Source Script Inventory

- `Assets\_syc_resource\_my_scripts\GameCtrl\FirewallManager.cs`
- `Assets\_syc_resource\_my_scripts\GameCtrl\GameManager.cs`
- `Assets\_syc_resource\_my_scripts\GameCtrl\QuitCtrl.cs`
- `Assets\_syc_resource\_my_scripts\KeepRigAlive.cs`
- `Assets\_syc_resource\_my_scripts\SYNC\ClientNetworkTransform.cs`
- `Assets\_syc_resource\_my_scripts\SYNC\GameServerSpawner.cs`
- `Assets\_syc_resource\_my_scripts\SYNC\LobbyCtrl.cs`
- `Assets\_syc_resource\_my_scripts\SYNC\MRTKNetTransform.cs`
- `Assets\_syc_resource\_my_scripts\SYNC\NetObjReporter.cs`
- `Assets\_syc_resource\_my_scripts\SYNC\NetworkActiveManager.cs`
- `Assets\_syc_resource\_my_scripts\SYNC\NormalObjReporter.cs`
- `Assets\_syc_resource\_my_scripts\SYNC2\GlobalSyncManager.cs`
- `Assets\_syc_resource\_my_scripts\SYNC2\SceneItemSync.cs`
- `Assets\_syc_resource\_my_scripts\UDP\DiscoveryData.cs`
- `Assets\_syc_resource\_my_scripts\UDP\ExampleNetworkDiscovery.cs`
- `Assets\_syc_resource\_my_scripts\UDP\ExampleNetworkDiscoveryHud.cs`
- `Assets\_syc_resource\_my_scripts\UDP\NetworkDiscovery.cs`
- `Assets\_syc_resource\_my_scripts\UI\AutoJoinManager.cs`
- `Assets\_syc_resource\_my_scripts\UI\ButtonManager.cs`
- `Assets\_syc_resource\_my_scripts\UI\RoomBrowserCtrl.cs`
- `Assets\_syc_resource\_my_scripts\UI\RoomListCell.cs`
- `Assets\_syc_resource\_my_scripts\UI\VolumeController.cs`
- `Assets\Demigiant\DOTweenPro\DOTweenAnimation.cs`
- `Assets\Demigiant\DOTweenPro\Editor\DOTweenAnimationInspector.cs`
- `Assets\Editor\AttachSceneItemSyncTool.cs`
- `Assets\Editor\Editor\Editor\AutoActiveTracker.cs`
- `Assets\Editor\Editor\Editor\AutoInteractableNetSetup.cs`
- `Assets\Editor\Editor\Editor\AutoPrefabAndRegister.cs`
- `Assets\Editor\Editor\Editor\Editor\AntiLagNetworkFixer.cs`
- `Assets\Editor\Editor\Editor\Editor\AutoNetworkParents.cs`
- `Assets\Editor\Editor\Editor\Editor\BluePrefabFixer.cs`
- `Assets\Editor\Editor\Editor\Editor\DebugNetworkArchiver.cs`
- `Assets\Editor\Editor\Editor\Editor\HybridNetworkFixer.cs`
- `Assets\Editor\Editor\Editor\Editor\ListCleanerAndFixer.cs`
- `Assets\Editor\Editor\Editor\Editor\NestedNetworkObjectCleaner.cs`
- `Assets\Editor\Editor\Editor\Editor\NetworkPrefabAutoLinker.cs`
- `Assets\Editor\Editor\Editor\Editor\NetworkSceneArchiver.cs`
- `Assets\Editor\Editor\Editor\Editor\SafeNetworkFixer.cs`
- `Assets\Editor\Editor\Editor\Editor\SlowAndSteadyFixer.cs`
- `Assets\Editor\Editor\Editor\Editor\UltimateNetworkFixer.cs`
- `Assets\Editor\Editor\Editor\FixHierarchyTool.cs`
- `Assets\Editor\Editor\Editor\MasterFixTool.cs`
- `Assets\Editor\Editor\Editor\RescueListTool.cs`
- `Assets\Editor\Editor\Editor\RootNodeNetSetup.cs`
- `Assets\Editor\Editor\Editor\ScopedNetSetup.cs`
- `Assets\Editor\Editor\Editor\SetupSyncTool.cs`
- `Assets\Editor\Editor\Editor\UltimateHashFixTool.cs`
- `Assets\Editor\Editor\UnpackAllPrefabsTool.cs`
- `Assets\Editor\Editor\UnpackSelectedPrefabsTool.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataProvider\DataProvider.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataProvider\DataProviderCreator.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataProvider\IDataProvider.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataProvider\IDataProviderHelper.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataProvider\ReadDataDependencyAssetEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataProvider\ReadDataFailureEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataProvider\ReadDataSuccessEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataProvider\ReadDataUpdateEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\DataStruct\TypeNamePair.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\EventPool\BaseEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\EventPool\EventPool.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\EventPool\EventPool.Event.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\EventPool\EventPoolMode.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkAction.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkEntry.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkException.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkFunc.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkLinkedList.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkLinkedListRange.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkModule.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkMultiDictionary.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\GameFrameworkSerializer.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\Log\GameFrameworkLog.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\Log\GameFrameworkLog.ILogHelper.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\Log\GameFrameworkLogLevel.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\ReferencePool\IReference.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\ReferencePool\ReferencePool.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\ReferencePool\ReferencePool.ReferenceCollection.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\ReferencePool\ReferencePoolInfo.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\TaskPool\ITaskAgent.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\TaskPool\StartTaskStatus.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\TaskPool\TaskBase.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\TaskPool\TaskInfo.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\TaskPool\TaskPool.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\TaskPool\TaskStatus.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\Variable\GenericVariable.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\Variable\Variable.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\Version\Version.cs`
- `Assets\GameFramework\Libraries\GameFramework\Base\Version\Version.IVersionHelper.cs`
- `Assets\GameFramework\Libraries\GameFramework\Config\ConfigManager.ConfigData.cs`
- `Assets\GameFramework\Libraries\GameFramework\Config\ConfigManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Config\IConfigHelper.cs`
- `Assets\GameFramework\Libraries\GameFramework\Config\IConfigManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Data\Constant.cs`
- `Assets\GameFramework\Libraries\GameFramework\Data\Data.cs`
- `Assets\GameFramework\Libraries\GameFramework\Data\DataInfo.cs`
- `Assets\GameFramework\Libraries\GameFramework\Data\DataManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Data\DataManager.DataStatus.cs`
- `Assets\GameFramework\Libraries\GameFramework\Data\IData.cs`
- `Assets\GameFramework\Libraries\GameFramework\Data\IDataManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataNode\DataNodeManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataNode\DataNodeManager.DataNode.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataNode\IDataNode.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataNode\IDataNodeManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataTable\DataTableBase.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataTable\DataTableManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataTable\DataTableManager.DataTable.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataTable\IDataRow.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataTable\IDataTable.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataTable\IDataTableHelper.cs`
- `Assets\GameFramework\Libraries\GameFramework\DataTable\IDataTableManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Debugger\DebuggerManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Debugger\DebuggerManager.DebuggerWindowGroup.cs`
- `Assets\GameFramework\Libraries\GameFramework\Debugger\IDebuggerManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Debugger\IDebuggerWindow.cs`
- `Assets\GameFramework\Libraries\GameFramework\Debugger\IDebuggerWindowGroup.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\Constant.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadAgentHelperCompleteEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadAgentHelperErrorEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadAgentHelperUpdateBytesEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadAgentHelperUpdateLengthEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadFailureEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadManager.DownloadAgent.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadManager.DownloadCounter.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadManager.DownloadCounter.DownloadCounterNode.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadManager.DownloadTask.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadManager.DownloadTaskStatus.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadStartEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadSuccessEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\DownloadUpdateEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\IDownloadAgentHelper.cs`
- `Assets\GameFramework\Libraries\GameFramework\Download\IDownloadManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\EntityManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\EntityManager.EntityGroup.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\EntityManager.EntityInfo.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\EntityManager.EntityInstanceObject.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\EntityManager.EntityStatus.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\EntityManager.ShowEntityInfo.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\HideEntityCompleteEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\IEntity.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\IEntityGroup.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\IEntityGroupHelper.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\IEntityHelper.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\IEntityManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\ShowEntityDependencyAssetEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\ShowEntityFailureEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\ShowEntitySuccessEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Entity\ShowEntityUpdateEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Event\EventManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\Event\GameEventArgs.cs`
- `Assets\GameFramework\Libraries\GameFramework\Event\IEventManager.cs`
- `Assets\GameFramework\Libraries\GameFramework\FileSystem\CommonFileSystemStream.cs`
- `Assets\GameFramework\Libraries\GameFramework\FileSystem\FileInfo.cs`
- `Assets\GameFramework\Libraries\GameFramework\FileSystem\FileSystem.BlockData.cs`
- `Assets\GameFramework\Libraries\GameFramework\FileSystem\FileSystem.cs`
- `Assets\GameFramework\Libraries\GameFramework\FileSystem\FileSystem.HeaderData.cs`
- `Assets\GameFramework\Libraries\GameFramework\FileSystem\FileSystem.StringData.cs`
- `Assets\GameFramework\Libraries\GameFramework\FileSystem\FileSystemAccess.cs`
- `Assets\GameFramework\Libraries\GameFramework\FileSystem\FileSystemManager.cs`

## Sample Business Prefab Pattern

- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤?meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤?prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭01鏈嶅姟鍣ㄧ綉缁滄惌寤虹粌涔犻.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭01鏈嶅姟鍣ㄧ綉缁滄惌寤虹粌涔犻.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭02鎿嶄綔娴嬭瘯1.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭02鎿嶄綔娴嬭瘯1.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭03杈撳叆IP.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭03杈撳叆IP.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭04瀹夎璐ф灦.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭04瀹夎璐ф灦.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭鏈変氦浜掔墿浣?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭鏈変氦浜掔墿浣?prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\01杞欢绯荤粺缁冧範棰?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\01杞欢绯荤粺缁冧範棰?prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\02鎿嶄綔娴嬭瘯.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\02鎿嶄綔娴嬭瘯.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\03鎿嶄綔娴嬭瘯.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\03鎿嶄綔娴嬭瘯.prefab.meta`

## Current Target Business Prefabs

- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤?meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤?prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭01鏈嶅姟鍣ㄧ綉缁滄惌寤虹粌涔犻.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭01鏈嶅姟鍣ㄧ綉缁滄惌寤虹粌涔犻.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭02纭欢杩炴帴鎿嶄綔.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭02纭欢杩炴帴鎿嶄綔.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\01杞欢绯荤粺缁冧範棰?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\01杞欢绯荤粺缁冧範棰?prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\02鎿嶄綔娴嬭瘯.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\02鎿嶄綔娴嬭瘯.prefab.meta`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\03鎿嶄綔娴嬭瘯.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\03鎿嶄綔娴嬭瘯.prefab.meta`

## Migration Phases

1. Compile-isolate legacy sync code under `Assets/APP/Modules/LegacyMrtkSync/Scripts` and fix namespace/package incompatibilities only where required.
2. Open copied reference scenes only as migration references; do not make them target build entry scenes by default.
3. Extract reusable lobby, discovery, sync, and object-reporting components into prefabs/modules that can be attached to current BPF scenes.
4. Recreate business flow nodes using the sample `BusinessPrefabs` pattern; keep names aligned across prefab filename, node data, and child references.
5. Rebind UI panels and 3D interaction steps through prefabs/scenes, then save prefab and scene changes in Unity.
6. Validate Unity compilation, package resolution, target startup scenes, and PICO runtime behavior before final commit.

## Current Blockers / Checks Needed

- Unity MCP is not connected in this run, so scene/prefab serialized edits were not attempted through the editor.
- The target currently compiles unknown until Unity reimports the copied code.
- Legacy project includes networking/package dependencies that may require target manifest adjustments after compilation errors are known.
- A manual Unity reimport or command-line Unity validation is required before committing these runtime files.

## Coverage Audit Added 2026-07-05

### Legacy GameMain Coverage

| Area | Status | Notes |
| --- | --- | --- |
| Assets/GameMain/Animations | Partially mapped in Assets/APP | old files=394, target candidate files=12 |
| Assets/GameMain/Configs | Not directly mapped | old files=18; needs staged migration after dependency review |
| Assets/GameMain/DataTables | Not directly mapped | old files=52; needs staged migration after dependency review |
| Assets/GameMain/Entity | Not directly mapped | old files=598; needs staged migration after dependency review |
| Assets/GameMain/Localization | Not directly mapped | old files=12; needs staged migration after dependency review |
| Assets/GameMain/Models | Partially mapped in Assets/APP | old files=790, target candidate files=746 |
| Assets/GameMain/Prefabs | Partially mapped in Assets/APP | old files=46, target candidate files=10 |
| Assets/GameMain/Res | Not directly mapped | old files=876; needs staged migration after dependency review |
| Assets/GameMain/Scenes | Not directly mapped | old files=78; needs staged migration after dependency review |
| Assets/GameMain/Scripts | Partially mapped in Assets/APP | old files=823, target candidate files=410 |
| Assets/GameMain/Sounds | Not directly mapped | old files=132; needs staged migration after dependency review |
| Assets/GameMain/UI | Not directly mapped | old files=115; needs staged migration after dependency review |

### External Runtime / Server Folders

| Area | Status | Notes |
| --- | --- | --- |
| Chatsever | External/runtime output retained as source reference | files=253; not copied into Unity Assets |
| netchat | External/runtime output retained as source reference | files=248; not copied into Unity Assets |
| n | External/runtime output retained as source reference | files=5; not copied into Unity Assets |

### Migrated Legacy Module Scripts

- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\GameCtrl\FirewallManager.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\GameCtrl\GameManager.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\GameCtrl\QuitCtrl.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\KeepRigAlive.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC\ClientNetworkTransform.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC\GameServerSpawner.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC\LobbyCtrl.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC\MRTKNetTransform.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC\NetObjReporter.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC\NetworkActiveManager.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC\NormalObjReporter.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC2\GlobalSyncManager.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\SYNC2\SceneItemSync.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UDP\DiscoveryData.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UDP\ExampleNetworkDiscovery.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UDP\ExampleNetworkDiscoveryHud.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UDP\NetworkDiscovery.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UI\AutoJoinManager.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UI\ButtonManager.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UI\RoomBrowserCtrl.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UI\RoomListCell.cs`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scripts\UI\VolumeController.cs`

### Migrated Legacy Reference Scenes

- `Assets\\APP\\Modules\\LegacyMrtkSync\Scenes\00Init\Init.unity`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scenes\02Lobby\Lobby.unity`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scenes\sync\MRTKsync.unity`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scenes\sync\MRTKsync1.unity`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scenes\sync\MRTKsync2.unity`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scenes\sync\MRTKsyncrigbroken.unity`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scenes\sync\MRTKsyncsuccess.unity`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Scenes\T04UDPSprate\UDPLobby.unity`

### Migrated Legacy Fonts / Font Assets

- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-10px-proportional-ttf-v2026.01.04\ark-pixel-10px-proportional-ja.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-10px-proportional-ttf-v2026.01.04\ark-pixel-10px-proportional-ko.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-10px-proportional-ttf-v2026.01.04\ark-pixel-10px-proportional-latin.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-10px-proportional-ttf-v2026.01.04\ark-pixel-10px-proportional-zh_cn.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-10px-proportional-ttf-v2026.01.04\ark-pixel-10px-proportional-zh_hk.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-10px-proportional-ttf-v2026.01.04\ark-pixel-10px-proportional-zh_tr.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-10px-proportional-ttf-v2026.01.04\ark-pixel-10px-proportional-zh_tw.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-10px-proportional-ttf-v2026.01.04\OFL.txt`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\ark-pixel-12px-monospaced-ja.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\ark-pixel-12px-monospaced-ko.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\ark-pixel-12px-monospaced-latin.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\ark-pixel-12px-monospaced-zh_cn SDF.asset`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\ark-pixel-12px-monospaced-zh_cn.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\ark-pixel-12px-monospaced-zh_hk.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\ark-pixel-12px-monospaced-zh_tr.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\ark-pixel-12px-monospaced-zh_tw.ttf`
- `Assets\\APP\\Modules\\LegacyMrtkSync\Resources\Fonts\ark-pixel-font-12px-monospaced-ttf-v2026.01.04\OFL.txt`

### Old GameMain Scripts Requiring Staged Review

- `Assets\GameMain\Scripts\AutoStartSeverChat.cs`
- `Assets\GameMain\Scripts\AutoStartSeverORClient.cs`
- `Assets\GameMain\Scripts\Base\GameEntry.Builtin.cs`
- `Assets\GameMain\Scripts\Base\GameEntry.cs`
- `Assets\GameMain\Scripts\Base\GameEntry.Custom.cs`
- `Assets\GameMain\Scripts\BuiltinData\BuiltinDataComponent.cs`
- `Assets\GameMain\Scripts\Camera\CameraControl.cs`
- `Assets\GameMain\Scripts\Camera\CameraInput.cs`
- `Assets\GameMain\Scripts\ChangeScene.cs`
- `Assets\GameMain\Scripts\CustomTank.cs`
- `Assets\GameMain\Scripts\Data\DataAssetPath.cs`
- `Assets\GameMain\Scripts\Data\DataBase.cs`
- `Assets\GameMain\Scripts\Data\DataPoolParam.cs`
- `Assets\GameMain\Scripts\Data\DataScene.cs`
- `Assets\GameMain\Scripts\Data\DataSetting.cs`
- `Assets\GameMain\Scripts\Data\DataSound.cs`
- `Assets\GameMain\Scripts\Data\DataUI.cs`
- `Assets\GameMain\Scripts\Data\Entity\DataEntity.cs`
- `Assets\GameMain\Scripts\Data\Entity\EntityData.cs`
- `Assets\GameMain\Scripts\Data\Entity\EntityGroupData.cs`
- `Assets\GameMain\Scripts\Data\Item\DataItem.cs`
- `Assets\GameMain\Scripts\Data\Item\ItemData.cs`
- `Assets\GameMain\Scripts\Data\Item\ItemGroupData.cs`
- `Assets\GameMain\Scripts\Data\Level\DataLevel.cs`
- `Assets\GameMain\Scripts\Data\Level\EnumLevelState.cs`
- `Assets\GameMain\Scripts\Data\Level\Level.cs`
- `Assets\GameMain\Scripts\Data\Level\LevelData.cs`
- `Assets\GameMain\Scripts\Data\Player\DataPlayer.cs`
- `Assets\GameMain\Scripts\DataTable\BinaryReaderExtension.cs`
- `Assets\GameMain\Scripts\DataTable\DataTableExtension.cs`
- `Assets\GameMain\Scripts\DataTable\DRAssetsPath.cs`
- `Assets\GameMain\Scripts\DataTable\DREntity.cs`
- `Assets\GameMain\Scripts\DataTable\DREntityGroup.cs`
- `Assets\GameMain\Scripts\DataTable\DRItem.cs`
- `Assets\GameMain\Scripts\DataTable\DRItemGroup.cs`
- `Assets\GameMain\Scripts\DataTable\DRLevel.cs`
- `Assets\GameMain\Scripts\DataTable\DRPoolParam.cs`
- `Assets\GameMain\Scripts\DataTable\DRScene.cs`
- `Assets\GameMain\Scripts\DataTable\DRSound.cs`
- `Assets\GameMain\Scripts\DataTable\DRSoundGroup.cs`
- `Assets\GameMain\Scripts\DataTable\DRSoundPlayParam.cs`
- `Assets\GameMain\Scripts\DataTable\DRUIForm.cs`
- `Assets\GameMain\Scripts\DataTable\DRUIGroup.cs`
- `Assets\GameMain\Scripts\Definition\BuildInfo.cs`
- `Assets\GameMain\Scripts\Definition\Constant\Constant.AssetPriority.cs`
- `Assets\GameMain\Scripts\Definition\Constant\Constant.Config.cs`
- `Assets\GameMain\Scripts\Definition\Constant\Constant.ProcedureData.cs`
- `Assets\GameMain\Scripts\Definition\Constant\Constant.Setting.cs`
- `Assets\GameMain\Scripts\Definition\VersionInfo.cs`
- `Assets\GameMain\Scripts\Editor\Camera\CameraControlEditor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableCodeGenerator.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableGenerator.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableGeneratorMenu.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.BoolProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.ByteProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.CharProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.Color32Processor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.ColorProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.CommentProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.DataProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.DataProcessorUtility.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.DateTimeProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.DecimalProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.DoubleProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.FloatProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.GenericDataProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.IdProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.IntArrayProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.IntProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.LongProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.QuaternionProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.RectProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.SByteProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.ShortProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.StringProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.UIntProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.ULongProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.UShortProcessor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.Vector2Processor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.Vector3Processor.cs`
- `Assets\GameMain\Scripts\Editor\DataTableGenerator\DataTableProcessor.Vector4Processor.cs`
- `Assets\GameMain\Scripts\Editor\EnumGenerator\DataTableEnumGenerator.cs`
- `Assets\GameMain\Scripts\Editor\GameFrameworkConfigs.cs`
- `Assets\GameMain\Scripts\Editor\LocalizationDictonaryGenerator\LocalizationDictonaryGenerator.cs`
- `Assets\GameMain\Scripts\Editor\LocalizationDictonaryGenerator\LocalizationDictonaryGeneratorMenu.cs`
- `Assets\GameMain\Scripts\Editor\MRTKBuildEventHandler.cs`
- `Assets\GameMain\Scripts\Editor\ResourceRuleEditor\ResourceRuleEditor.cs`
- `Assets\GameMain\Scripts\Editor\ResourceRuleEditor\ResourceRuleEditorData.cs`
- `Assets\GameMain\Scripts\Editor\ResourceRuleEditor\ResourceRuleEditorUtility.cs`
- `Assets\GameMain\Scripts\Entity\Entity.cs`
- `Assets\GameMain\Scripts\Entity\EntityData.cs`
- `Assets\GameMain\Scripts\Entity\EntityData\EDsimpleUI.cs`
- `Assets\GameMain\Scripts\Entity\EntityData\EneityDataPlayer.cs`
- `Assets\GameMain\Scripts\Entity\EntityData\EntityDataFollower.cs`
- `Assets\GameMain\Scripts\Entity\EntityData\EntityDataRadiusVisualiser.cs`
- `Assets\GameMain\Scripts\Entity\EntityExtension.cs`
- `Assets\GameMain\Scripts\Entity\EntityLogic\EntityRadiusVisualizer.cs`
- `Assets\GameMain\Scripts\Entity\EntityLogic\EntitySimpleUI.cs`
- `Assets\GameMain\Scripts\Entity\EntityLogicEx.cs`
- `Assets\GameMain\Scripts\Enum\EnumEntity.cs`
- `Assets\GameMain\Scripts\Enum\EnumItem.cs`
- `Assets\GameMain\Scripts\Enum\EnumSound.cs`
- `Assets\GameMain\Scripts\Enum\EnumUIForm.cs`
- `Assets\GameMain\Scripts\Event\ChangeSceneEventArgs.cs`
- `Assets\GameMain\Scripts\Event\GameoverEventArgs.cs`
- `Assets\GameMain\Scripts\Event\HideEntityInLevelEventArgs.cs`
- `Assets\GameMain\Scripts\Event\LevelStateChangeEventArgs.cs`
- `Assets\GameMain\Scripts\Event\LoadLevelEventArgs.cs`
- `Assets\GameMain\Scripts\Event\LoadLevelFinishEventArgs.cs`
- `Assets\GameMain\Scripts\Event\MRShowEntityArgs.cs`
- `Assets\GameMain\Scripts\Event\ReloadLevelEventArgs.cs`
- `Assets\GameMain\Scripts\Event\ShowEntityInLevelEventArgs.cs`
- `Assets\GameMain\Scripts\Extension\EntityLoader.cs`
- `Assets\GameMain\Scripts\Extension\EventSubscriber.cs`
- `Assets\GameMain\Scripts\Extension\ItemLoader.cs`
- `Assets\GameMain\Scripts\FrameWorks\Base\AppStart.cs`
- `Assets\GameMain\Scripts\FrameWorks\Base\BaseClsTemplate.cs`
- `Assets\GameMain\Scripts\FrameWorks\Base\BBehaviourSingleton.cs`
- `Assets\GameMain\Scripts\FrameWorks\Base\HoloBase.cs`
- `Assets\GameMain\Scripts\FrameWorks\Base\Singleton.cs`
- `Assets\GameMain\Scripts\FrameWorks\Base\UBehhaviourRoot.cs`
- `Assets\GameMain\Scripts\FrameWorks\Base\UIBaseBehavior.cs`
- `Assets\GameMain\Scripts\FrameWorks\Base\UnitySingleton.cs`
- `Assets\GameMain\Scripts\FrameWorks\Event\EventCenter.cs`
- `Assets\GameMain\Scripts\FrameWorks\NetWork\Http\DHttpRequest.cs`
- `Assets\GameMain\Scripts\FrameWorks\NetWork\Http\HttpConnection.cs`
- `Assets\GameMain\Scripts\FrameWorks\NetWork\Http\HttpRequestImp.cs`
- `Assets\GameMain\Scripts\FrameWorks\NetWork\Http\IHttpRequest.cs`
- `Assets\GameMain\Scripts\FrameWorks\NetWork\Http\WWWEx.cs`
- `Assets\GameMain\Scripts\FrameWorks\Pool\ObjectPool.cs`
- `Assets\GameMain\Scripts\FrameWorks\Pool\ObjectPoolContainer.cs`
- `Assets\GameMain\Scripts\FrameWorks\Pool\PoolManager.cs`
- `Assets\GameMain\Scripts\FrameWorks\Resource\SceneLoader.cs`
- `Assets\GameMain\Scripts\FrameWorks\UI\UI.cs`
- `Assets\GameMain\Scripts\FrameWorks\UI\UIManager.cs`
- `Assets\GameMain\Scripts\GetOperationAuthority.cs`
- `Assets\GameMain\Scripts\HjBzqTaskController.cs`
- `Assets\GameMain\Scripts\Item\ItemExtension.cs`
- `Assets\GameMain\Scripts\Item\ItemLevelSelectionButton.cs`
- `Assets\GameMain\Scripts\Item\ItemLogicEx.cs`
- `Assets\GameMain\Scripts\JYJ\AnimationDaKong.cs`
- `Assets\GameMain\Scripts\JYJ\AnimationDingZi.cs`
- `Assets\GameMain\Scripts\JYJ\banshouAnimation.cs`
- `Assets\GameMain\Scripts\JYJ\banshoutishi.cs`
- `Assets\GameMain\Scripts\JYJ\chadian.cs`
- `Assets\GameMain\Scripts\JYJ\chongdianzhuang.cs`
- `Assets\GameMain\Scripts\JYJ\chongdianzhuangkaigai.cs`
- `Assets\GameMain\Scripts\JYJ\chuiziAnimation.cs`
- `Assets\GameMain\Scripts\JYJ\chuizitishi.cs`
- `Assets\GameMain\Scripts\JYJ\DakongXifu.cs`
- `Assets\GameMain\Scripts\JYJ\dianxian.cs`
- `Assets\GameMain\Scripts\JYJ\DianzuanAnimationControl.cs`
- `Assets\GameMain\Scripts\JYJ\dianzuanJuliXiFu.cs`
- `Assets\GameMain\Scripts\JYJ\dinggaitishi.cs`
- `Assets\GameMain\Scripts\JYJ\HJRC\AttachToParent.cs`
- `Assets\GameMain\Scripts\JYJ\HJRC\collision.cs`
- `Assets\GameMain\Scripts\JYJ\HJRC\instantiation.cs`
- `Assets\GameMain\Scripts\JYJ\HJRC\Panding.cs`
- `Assets\GameMain\Scripts\JYJ\liujiaoluosi.cs`
- `Assets\GameMain\Scripts\JYJ\MoveControl.cs`
- `Assets\GameMain\Scripts\JYJ\pengzhangguan4_xifu.cs`
- `Assets\GameMain\Scripts\JYJ\pengzhangluosi4_xifu.cs`
- `Assets\GameMain\Scripts\JYJ\task_kaigai.cs`
- `Assets\GameMain\Scripts\JYJ\TaskDaKong.cs`
- `Assets\GameMain\Scripts\JYJ\Taskdianzuan.cs`
- `Assets\GameMain\Scripts\JYJ\tianxian2_xifu.cs`
- `Assets\GameMain\Scripts\JYJ\tianxiantishi.cs`
- `Assets\GameMain\Scripts\JYJ\tiema.cs`
- `Assets\GameMain\Scripts\JYJ\tishibobao.cs`
- `Assets\GameMain\Scripts\JYJ\xifu.cs`
- `Assets\GameMain\Scripts\Level\IPause.cs`
- `Assets\GameMain\Scripts\Level\LevelControl.cs`
- `Assets\GameMain\Scripts\Level\LevelManager.cs`
- `Assets\GameMain\Scripts\Level\LevelPath.cs`
- `Assets\GameMain\Scripts\Localization\JsonLocallizationHelper.cs`
- `Assets\GameMain\Scripts\Localization\LocalizationSerializableObject.cs`
- `Assets\GameMain\Scripts\LSL\DictationHandler.cs`
- `Assets\GameMain\Scripts\LSL\InputField_Button.cs`
- `Assets\GameMain\Scripts\LSL\InputField_InputField.cs`
- `Assets\GameMain\Scripts\LSL\UGUIInputField_InputField.cs`
- `Assets\GameMain\Scripts\LSL\VideoPlayerTest.cs`
- `Assets\GameMain\Scripts\Menu\IntervalParticleSystemPlayer.cs`
- `Assets\GameMain\Scripts\Menu\Rotator.cs`
- `Assets\GameMain\Scripts\Multiplayer\NetObjectSpawn.cs`
- `Assets\GameMain\Scripts\Multiplayer\NetworkedObject.cs`
- `Assets\GameMain\Scripts\NewBehaviourScript.cs`
- `Assets\GameMain\Scripts\NewS\IpTrueOrFalse.cs`
- `Assets\GameMain\Scripts\NewS\NewTaskOne.cs`
- `Assets\GameMain\Scripts\NewS\PersonMove.cs`
- `Assets\GameMain\Scripts\NewS\UpanBack.cs`
- `Assets\GameMain\Scripts\ObjofBagDefine.cs`
- `Assets\GameMain\Scripts\ObjofBagDefineATTACH.cs`
- `Assets\GameMain\Scripts\OnTrigger.cs`
- `Assets\GameMain\Scripts\PersonDefine.cs`
- `Assets\GameMain\Scripts\PQ\ACController.cs`
- `Assets\GameMain\Scripts\PQ\APController.cs`
- `Assets\GameMain\Scripts\PQ\AvatarButton.cs`
- `Assets\GameMain\Scripts\PQ\AvatarSys.cs`
- `Assets\GameMain\Scripts\PQ\BJBController.cs`
- ... truncated; total 378 items

### Target Business Prefabs To Preserve

- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭01鏈嶅姟鍣ㄧ綉缁滄惌寤虹粌涔犻.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭02纭欢杩炴帴鎿嶄綔.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\01杞欢绯荤粺缁冧範棰?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\02鎿嶄綔娴嬭瘯.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\03鎿嶄綔娴嬭瘯.prefab`

### Sample Business Prefab Pattern Reference

- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭01鏈嶅姟鍣ㄧ綉缁滄惌寤虹粌涔犻.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭02鎿嶄綔娴嬭瘯1.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭03杈撳叆IP.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭04瀹夎璐ф灦.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\01鏈嶅姟鍣ㄧ綉缁滄惌寤篭鏈変氦浜掔墿浣?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\01杞欢绯荤粺缁冧範棰?prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\02鎿嶄綔娴嬭瘯.prefab`
- `Assets\Resources\BusinessPrefabs\MR娣峰悎鐜板疄澶氫汉鍗忎綔骞冲彴\01璐у埌浜烘嫞閫夌數鍟嗕粨鍌ㄥ疄鏂藉畨瑁呬笌璋冭瘯瀹炶绯荤粺\02杞欢绯荤粺瀹夎\03鎿嶄綔娴嬭瘯.prefab`

## Validation Status

- Static checks passed for missing type declarations inside `Assets/APP/Modules/LegacyMrtkSync/Scripts`.
- No duplicate fully-qualified types were found for the copied `LegacyMrtkSync` scripts.
- Directory `.meta` files were created for new migration folders.
- Unity batch validation is blocked because `D:\UnityProject\git_BPF` is currently open in another Unity instance.
- Next validation requires closing the open `git_BPF` Unity Editor or using the already-open editor to reimport and inspect Console errors.
## Package Resolution Fix Added 2026-07-05

- `com.coplaydev.unity-mcp` was moved from an invalid `Library/PackageCache` file dependency to `Packages/LocalPackages/com.coplaydev.unity-mcp`.
- `Packages/manifest.json` now references `file:LocalPackages/com.coplaydev.unity-mcp`, which is outside `Assets`, `Library`, `ProjectSettings`, and `UserSettings`.
- Unity should re-resolve packages after reopening or reimporting the project.
## 04 Scene Integration Added 2026-07-05

- Added `LegacyMrtkSync04Bootstrap` under `Assets/APP/Modules/LegacyMrtkSync/Scripts/Runtime`.
- Added `LegacyMrtkSync04Root` to `Assets/A_MRNet/Sence/04SingleplayerScene.unity` as a dedicated scene-level networking root.
- The root keeps existing BPF business objects intact and groups NGO `NetworkManager`, `UnityTransport`, legacy UDP discovery, auto-join, and the 04 bootstrap on one object.
- Auto host/client startup is disabled by default so business flow can decide when to start networking.
- Default transport port is `7777`; UDP discovery broadcast port is `47777`.

