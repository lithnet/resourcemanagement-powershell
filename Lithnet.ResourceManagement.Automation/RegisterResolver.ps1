$OnAssemblyResolve = [System.ResolveEventHandler] {
  param($sender, $e)
  
	if (!($e.Name.StartsWith("Microsoft.ResourceManagement")))
	{
		return $null
	}

	if(([appdomain]::currentdomain.getassemblies() | Where {$_ -match $AssemblyName}) -eq $null)
	{
		try
		{
			return [System.Reflection.Assembly]::LoadFromPartialName('Microsoft.ResourceManagement');
		}
		catch
		{
			try
			{
				return [System.Reflection.Assembly]::Load('Microsoft.ResourceManagement.dll');
			}
			catch
			{
			}
		}
	}
	else
	{
		return $null;
	}
	
<#
  try
  {
    return [System.Reflection.Assembly]::LoadWithPartialName('Microsoft.ResourceManagement');
  }
  catch
  {
	try
	{
		return [System.Reflection.Assembly]::Load('Microsoft.ResourceManagement.dll');
	}
	catch
	{
	}
  }
 #>
  return $null
}

[System.AppDomain]::CurrentDomain.add_AssemblyResolve($OnAssemblyResolve)
