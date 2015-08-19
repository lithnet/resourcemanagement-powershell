$OnAssemblyResolve = [System.ResolveEventHandler] {
  param($sender, $e)

  if (!($e.Name.StartsWith("Microsoft.ResourceManagement")))
  {
	return $null
  }

  try
  {
    return [System.Reflection.Assembly]::LoadWithPartialName('Microsoft.ResourceManagement');
  }
  catch
  {
	return [System.Reflection.Assembly]::Load('Microsoft.ResourceManagement.dll');
  }
 
  return $null
}

[System.AppDomain]::CurrentDomain.add_AssemblyResolve($OnAssemblyResolve)
