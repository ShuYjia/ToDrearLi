using HuaXian.ExportRuntime;

public static class TaskStateSync
{
    public static void SetTask(LoadSceneObj.Tesks task)
    {
        if (LoadSceneObj.task == task)
        {
            return;
        }

        LoadSceneObj.task = task;
    }
}
