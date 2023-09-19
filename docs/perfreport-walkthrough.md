# Performance Report Advanced Features

## Finding a commit range using a graph

Link to a [Test Report](https://pvscmdupload.blob.core.windows.net/autofilereport/autofilereports/09_05_2023/refs/heads/main_x64_Windows%2010.0.19042/amd_Regression/System.Reflection.Attributes.html)

When looking at a report start by clicking on the small chart on the left side of the row.

<img width="1871" alt="image" src="https://github.com/dotnet/runtime/assets/9439069/0428d1db-15fc-4440-b587-9e37ec25baa9">

This will bring up a larger version of the graph. You can zoom, pan, and select an area for zoom using the controls on the graph overlay.

<img width="1874" alt="image" src="https://github.com/dotnet/runtime/assets/9439069/7a36cfe6-b8e5-417f-a725-e45def66f223">

Use the selection tool to focus in on the part of the graph that shows the regression.

<img width="753" alt="image" src="https://github.com/dotnet/runtime/assets/9439069/13dcea0f-5055-4d26-8bff-15d3154e4ef7">

After that it will be easy to click the point right before when the regression occurred. When you click that point you will get a fly-out that has several options. `Set Baseline` `Set Compare` and `Clear Comparison`. It works best to click your baseline point first and then click `Set Baseline`.

<img width="992" alt="image" src="https://github.com/dotnet/runtime/assets/9439069/bfbf35c2-2639-488f-a753-367ff0169c08">

Once you have the baseline set you can close the fly-out by clicking on the red X.

<img width="763" alt="image" src="https://github.com/dotnet/runtime/assets/9439069/be7b5d01-a1ac-4caf-94e3-ad0542e4f74f">

Now, click the compare point, in this case you want to click the point that is closest to the baseline point that shows the regression. 

<img width="830" alt="image" src="https://github.com/dotnet/runtime/assets/9439069/dd86cd1a-6079-4022-af04-1f46df3c595f">

After that click the `Set Compare` button.

<img width="947" alt="image" src="https://github.com/dotnet/runtime/assets/9439069/a7b10a83-ad99-4672-aecb-e5312556b045">

This will bring up a link to the runtime repo that will do a comparison between the git commit hashes of the two points that were set as the baseline and compare.

<img width="896" alt="image" src="https://github.com/dotnet/runtime/assets/9439069/25e48d06-6faf-4153-801d-fe40b28d3354">
