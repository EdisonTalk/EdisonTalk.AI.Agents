using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace EDT.Agent.Shared.FunctionCallers;

internal class PreTrainingPlugin
{
    [KernelFunction, Description("当工作流程完成，没有更多的函数需要调用时，调用这个函数")]
    public string Finished([Description("总结已完成的工作和结果，尽量简洁明了。")] string finalmessage)
    {
        return string.Empty;
        //no actual implementation, for internal routing only
    }

    [KernelFunction, Description("获取用户飞船的名称")]
    public string GetMySpaceshipName()
    {
        return "长征七号";
    }

    [KernelFunction, Description("启动飞船")]
    public void StartSpaceship([Description("启动的飞船的名字")] string ship_name)
    {
        //no actual implementation, for internal routing only
    }
}