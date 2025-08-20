


#include "../common/common.shader"

#include "shadow_mapping.shader"


#define PI 3.14159265359


// Simulates microfacet model (Trowbridge-Reitz GGX)
float normalDistribution(vec3 normal, vec3 h, float roughness)
{
	float a = roughness * roughness;
	float a2 = a * a;
	float ndoth = max(dot(normal, h), 0.0);

	float denom = ndoth * ndoth * (a2 - 1.0) + 1.0;

	return a2 / (PI * denom * denom);
}

// Self shadowing of microfacets (Schlick-GGX)
float geometryGGX(float ndotv, float k)
{
	return ndotv / (ndotv * (1.0 - k) + k);
}

float geometrySmith(vec3 normal, vec3 view, vec3 wi, float roughness)
{
	// Roughness remapping
	float r = roughness + 1.0;
	float k = r * r / 8.0;

	float ndotv = max(dot(normal, view), 0.0); // TODO precalculate this
	float ndotl = max(dot(normal, wi), 0.0); // TODO precalculate this

	return geometryGGX(ndotv, k) * geometryGGX(ndotl, k);
}

// Variant fresnel equation taking the roughness into account;
vec3 fresnel2(float hdotv, vec3 f0, float roughness)
{
	//return f0 + (1.0 - f0) * pow(clamp(1.0 - hdotv, 0.0, 1.0), 5.0);
	return f0 + (max(vec3_splat(1.0 - roughness), f0) - f0) * pow(1.0 - hdotv, 5.0);
}

// Radiance calculation for radial flux over the angle w
vec3 L(vec3 color, float distanceSq)
{
	//float attenuation = 1.0 / (distanceSq * 4);

	//float attenuation = 1.0 / (1.0 + 4 * distanceSq);

	float dist = sqrt(distanceSq);
	float attenuation = 1.0 / (1.0 + 1 * dist + 2 * distanceSq);

	//float distance = sqrt(distanceSq);
    //float attenuation = max(1 - distance / lightRadius, 0);
	//attenuation = attenuation * attenuation;
	//attenuation += 0.05 * exp(-0.2 * distance);

	//float radius = 0.1;
	//float distance = sqrt(distanceSq);
	//float attenuation = 2 / (radius * radius) * (1 - distance / sqrt(distanceSq + radius * radius));

	vec3 radiance = color * attenuation;

	float maxComponent = max(radiance.r, max(radiance.g, radiance.b));
	radiance *= max(1 - 0.01 / maxComponent, 0);

	return radiance;
}

// Point light indirect specular lighting
vec3 RenderPointLight(vec3 position, vec3 normal, vec3 view, vec3 albedo, float roughness, float metallic, vec3 lightPosition, vec3 lightColor)
{
	vec3 f0 = mix(vec3_splat(0.04), albedo, metallic);
	vec3 fLambert = albedo / PI;

	// Per light radiance
	vec3 toLight = lightPosition - position;
	vec3 wi = normalize(toLight);
	vec3 h = normalize(view + wi);

	float distanceSq = dot(toLight, toLight);
	vec3 radiance = L(lightColor, distanceSq);

	// Cook-Torrance BRDF
	float d = normalDistribution(normal, h, roughness);
	float g = geometrySmith(normal, view, wi, roughness);
	vec3 f = fresnel2(max(dot(h, view), 0.0), f0, roughness);
	vec3 numerator = d * f * g;
	float denominator = 4.0 * max(dot(view, normal), 0.0) * max(dot(wi, normal), 0.0) + 0.0001;
	vec3 specular = numerator / max(denominator, 0.0001);

	vec3 ks = f;
	vec3 kd = (1.0 - ks) * (1.0 - metallic);

	float ndotwi = max(dot(wi, normal), 0.0);
	float shadow = 1.0; // Shadow mapping

	vec3 s = (specular + fLambert * kd) * radiance * ndotwi * shadow;

	return s;
}

/*
// Point light indirect specular lighting
vec3 RenderPointLightShadow(vec3 position, vec3 normal, vec3 view, vec3 albedo, float roughness, float metallic, vec3 lightPosition, vec3 lightColor, samplerCubeShadow shadowMap, float shadowMapNear)
{
	vec3 f0 = mix(vec3_splat(0.04), albedo, metallic);
	vec3 fLambert = albedo / PI;

	// Per light radiance
	vec3 toLight = lightPosition - position;
	vec3 wi = normalize(toLight);
	vec3 h = normalize(view + wi);

	float distanceSq = dot(toLight, toLight);
	vec3 radiance = L(lightColor, distanceSq);

	// Cook-Torrance BRDF
	float d = normalDistribution(normal, h, roughness);
	float g = geometrySmith(normal, view, wi, roughness);
	vec3 f = fresnel2(max(dot(h, view), 0.0), f0, roughness);
	vec3 numerator = d * f * g;
	float denominator = 4.0 * max(dot(view, normal), 0.0) * max(dot(wi, normal), 0.0) + 0.0001;
	vec3 specular = numerator / max(denominator, 0.0001);

	vec3 ks = f;
	vec3 kd = (1.0 - ks) * (1.0 - metallic);

	float ndotwi = max(dot(wi, normal), 0.0);
	float shadow = CalculatePointShadow(position, lightPosition, shadowMap, shadowMapNear, 30);

	vec3 s = (specular + fLambert * kd) * radiance * ndotwi * shadow;

	return s;
}
*/

// Directional light indirect specular lighting
vec3 RenderDirectionalLight(vec3 position, vec3 normal, vec3 view, float distance, vec3 albedo, float roughness, float metallic, vec3 lightDirection, vec3 lightColor, sampler2DShadow shadowMap0, float shadowMapFar0, mat4 toLightSpace0, sampler2DShadow shadowMap1, float shadowMapFar1, mat4 toLightSpace1, sampler2DShadow shadowMap2, float shadowMapFar2, mat4 toLightSpace2, vec4 fragCoord)
{
	vec3 f0 = mix(vec3_splat(0.04), albedo, metallic);
	vec3 fLambert = albedo / PI;

	// Per light radiance
	vec3 wi = -lightDirection;
	vec3 h = normalize(view + wi);

	vec3 radiance = lightColor;

	// Cook-Torrance BRDF
	float d = normalDistribution(normal, h, roughness);
	float g = geometrySmith(normal, view, wi, roughness);
	vec3 f = fresnel2(max(dot(h, view), 0.0), f0, roughness);
	vec3 numerator = d * f * g;
	float denominator = 4.0 * max(dot(view, normal), 0.0) * max(dot(wi, normal), 0.0) + 0.0001;
	vec3 specular = numerator / max(denominator, 0.0001);

	vec3 ks = f;
	vec3 kd = (1.0 - ks) * (1.0 - metallic);

	float ndotwi = max(dot(wi, normal), 0.0);

	// Shadow mapping
	int cascadeID = distance < shadowMapFar0 ? 0 : distance < shadowMapFar1 ? 1 : 2;
	float shadow = 1;
	switch (cascadeID)
	{
	case 0: shadow = CalculateDirectionalShadow(position, distance, shadowMap0, shadowMapFar0, toLightSpace0, 0.0, fragCoord); break;
	case 1: shadow = CalculateDirectionalShadow(position, distance, shadowMap1, shadowMapFar1, toLightSpace1, 0.0, fragCoord); break;
	case 2: shadow = CalculateDirectionalShadow(position, distance, shadowMap2, shadowMapFar2, toLightSpace2, 1.0, fragCoord); break;
	//case 0: radiance = vec3(1, 0, 0); break;
	//case 1: radiance = vec3(0, 1, 0); break;
	//case 2: radiance = vec3(0, 0, 1); break;
	}
	
	//vec3 s = specular * shadow + fLambert * kd * radiance * ndotwi * shadow;
	vec3 s = (specular + fLambert * kd) * radiance * ndotwi * shadow;

	return s;
}

/*
// Environment mapping
vec3 RenderEnvironmentMap(vec3 normal, vec3 view, vec3 albedo, float roughness, float metallic, float ao, samplerCube environmentMap, float environmentMapIntensity)
{
	vec3 f0 = mix(vec3_splat(0.04), albedo, metallic);

	vec3 ks = fresnel2(max(dot(normal, view), 0.0), f0, roughness);
	vec3 kd = (1.0 - ks) * (1.0 - metallic);

	float maxLod = log2(textureSize(environmentMap, 0).x);

	vec3 irradiance = textureCubeLod(environmentMap, normal, maxLod).rgb * environmentMapIntensity;
	vec3 diffuse = irradiance * albedo;

	vec3 r = reflect(-view, normal);
	float minLod = max(maxLod - 8, 0);
	float lodFactor = (1.0 - exp(-roughness * 2.5)) * (maxLod - minLod) + minLod;
	vec3 prefiltered = textureCubeLod(environmentMap, r, lodFactor).rgb * environmentMapIntensity;

	vec2 brdfInteg = vec2(1.0, 0.0);
	vec3 specular = prefiltered * (ks * brdfInteg.r + brdfInteg.g);

	vec3 ambient = kd * diffuse * ao + specular * ao;

	return ambient;
}

// Environment mapping
vec3 RenderEnvironmentMapParallax(vec3 position, vec3 normal, vec3 view, vec3 albedo, float roughness, float metallic, float ao, samplerCube environmentMap, vec3 environmentMapPosition, vec3 environmentMapSize, vec3 environmentMapOrigin, float environmentMapIntensity){
	vec3 f0 = mix(vec3_splat(0.04), albedo, metallic);

	vec3 ks = fresnel2(max(dot(normal, view), 0.0), f0, roughness);
	vec3 kd = (1.0 - ks) * (1.0 - metallic);
	
	float maxLod = log2(textureSize(environmentMap, 0).x);

	vec3 irradiance = SampleCubemapParallax(environmentMap, normal, maxLod, environmentMapPosition, environmentMapSize, environmentMapOrigin, position).rgb * environmentMapIntensity;
	vec3 diffuse = irradiance * albedo;

	vec3 r = reflect(-view, normal);
	float lodFactor = (1.0 - exp(-roughness * 12.0)) * maxLod;
	vec3 prefiltered = SampleCubemapParallax(environmentMap, r, lodFactor, environmentMapPosition, environmentMapSize, environmentMapOrigin, position).rgb * environmentMapIntensity;

	vec2 brdfInteg = vec2(1.0, 0.0);
	vec3 specular = prefiltered * (ks * brdfInteg.r + brdfInteg.g);

	vec3 ambient = kd * diffuse * ao + specular * ao; // + kd * 0.2 * diffuse * ao;

	return ambient;
}
*/

float DistanceToBox(vec3 position, vec3 size, vec3 p)
{
	p -= position;

	vec3 scale = size / 2.0;
	p /= scale;

	vec3 ap = abs(p);
	if (ap.x <= 1.0)
	{
		if (ap.y <= 1.0)
		{
			if (ap.z <= 1.0)
			{
				vec3 pp = scale - ap * scale;
				return -min(min(pp.x, pp.y), pp.z);
			}
			else
				return (ap.z - 1.0) * scale.z;
		}
		else
		{
			if (ap.z <= 1.0)
				return (ap.y - 1.0) * scale.y;
			else
				return length((ap.yz - 1.0) * scale.yz);
		}
	}
	else
	{
		if (ap.y <= 1.0)
		{
			if (ap.z <= 1.0)
				return (ap.x - 1.0) * scale.x;
			else
				return length((ap.xz - 1.0) * scale.xz);
		}
		else
		{
			if (ap.z <= 1.0)
				return length((ap.xy - 1.0) * scale.xy);
			else
				return length((ap - 1.0) * scale);
		}
	}

	return 0.0;
}

vec4 CalculateReflectionWeights(vec3 position,
	vec3 reflectionPosition0, vec3 reflectionSize0, vec3 reflectionOrigin0,
	vec3 reflectionPosition1, vec3 reflectionSize1, vec3 reflectionOrigin1,
	vec3 reflectionPosition2, vec3 reflectionSize2, vec3 reflectionOrigin2,
	vec3 reflectionPosition3, vec3 reflectionSize3, vec3 reflectionOrigin3)
{
	const float ENVIRMAP_FADEOUT_DISTANCE = 0.998;

	float distance0 = DistanceToBox(reflectionPosition0, reflectionSize0, position);
	float fadeOut0 = max(1.0 - distance0 / ENVIRMAP_FADEOUT_DISTANCE, 0.0);

	float distance1 = DistanceToBox(reflectionPosition1, reflectionSize1, position);
	float fadeOut1 = max(1.0 - distance1 / ENVIRMAP_FADEOUT_DISTANCE, 0.0);

	float distance2 = DistanceToBox(reflectionPosition2, reflectionSize2, position);
	float fadeOut2 = max(1.0 - distance2 / ENVIRMAP_FADEOUT_DISTANCE, 0.0);

	float distance3 = DistanceToBox(reflectionPosition3, reflectionSize3, position);
	float fadeOut3 = max(1.0 - distance3 / ENVIRMAP_FADEOUT_DISTANCE, 0.0);

	return vec4(fadeOut0, fadeOut1, fadeOut2, fadeOut3);
}

vec4 SampleCubemapParallax(samplerCube cubemap, vec3 position, vec3 direction, float lod, vec3 cubemapPosition, vec3 cubemapSize, vec3 cubemapOrigin)
{
	vec3 boxMin = cubemapPosition - 0.5 * cubemapSize;
	vec3 boxMax = cubemapPosition + 0.5f * cubemapSize;

	vec3 firstPlaneIntersect = (boxMax - position) / direction;
	vec3 secondPlaneIntersect = (boxMin - position) / direction;

	vec3 furthestPlane = max(firstPlaneIntersect, secondPlaneIntersect);
	float distance = min(min(furthestPlane.x, furthestPlane.y), furthestPlane.z);
	distance = abs(distance);

	vec3 intersection = position + direction * distance;
	vec3 boxToIntersection = intersection - cubemapOrigin;

	return textureCubeLod(cubemap, boxToIntersection, lod);
}

vec4 RenderReflectionsParallax(vec3 position, vec3 normal, vec3 view, vec3 albedo, float roughness, float metallic, samplerCube environmentMap, vec3 cubemapPosition, vec3 cubemapSize, vec3 cubemapOrigin)
{
	float maxCubemapLod = log2(textureSize(environmentMap, 0).x);

	vec3 irradiance = SampleCubemapParallax(environmentMap, position, normal, maxCubemapLod, cubemapPosition, cubemapSize, cubemapOrigin).rgb;

	vec3 diffuse = irradiance * albedo;

	vec3 f0 = mix(vec3_splat(0.04), albedo, metallic);
	vec3 ks = fresnel2(max(dot(normal, view), 0.0), f0, roughness);
	vec3 kd = (1.0 - ks) * (1.0 - metallic);

	vec3 r = reflect(-view, normal);
	//float lodFactor = 1 - exp(-(1 - roughness) * 0.25);
	float lodFactor = pow(1 - roughness, 3);
	float lod = maxCubemapLod - lodFactor * 10;

	vec3 prefiltered = SampleCubemapParallax(environmentMap, position, r, lod, cubemapPosition, cubemapSize, cubemapOrigin).rgb;

	vec2 brdfInteg = vec2(1.0, 0.0);
	vec3 specular = prefiltered * (ks * brdfInteg.r + brdfInteg.g);

	vec3 ambient = kd * diffuse + specular;

	vec3 ap = abs(position - cubemapPosition) - 0.5 * cubemapSize;
	float distanceFromBorder = max(ap.x, max(ap.y, ap.z));
	float fadeout = 1 - smoothstep(0, 1, distanceFromBorder);

	return vec4(ambient, fadeout);
}

vec3 SampleEnvironmentIrradiance(vec3 position, vec3 normal, samplerCube environmentMap, float environmentIntensity)
{
	return textureCubeLod(environmentMap, normal, log2(textureSize(environmentMap, 0).x)).rgb * environmentIntensity;
}

vec3 SampleEnvironmentPrefiltered(vec3 position, vec3 normal, vec3 view, float roughness, samplerCube environmentMap, float environmentIntensity)
{
	vec3 r = reflect(-view, normal);
	float lodFactor = 1.0 - exp(-roughness * 12);

	return textureCubeLod(environmentMap, r, lodFactor * log2(textureSize(environmentMap, 0).x)).rgb * environmentIntensity;
}

vec3 RenderEnvironment(vec3 position, vec3 normal, vec3 view, vec3 albedo, float roughness, float metallic, samplerCube environmentMap, float environmentIntensity)
{
	vec3 irradiance = SampleEnvironmentIrradiance(position, normal, environmentMap, environmentIntensity);

	vec3 diffuse = irradiance * albedo;

	vec3 f0 = mix(vec3_splat(0.04), albedo, metallic);
	vec3 ks = fresnel2(max(dot(normal, view), 0.0), f0, roughness);
	vec3 kd = (1.0 - ks) * (1.0 - metallic);

	vec3 prefiltered = SampleEnvironmentPrefiltered(position, normal, view, roughness, environmentMap, environmentIntensity);

	vec2 brdfInteg = vec2(1.0, 0.0);
	vec3 specular = prefiltered * (ks * brdfInteg.r + brdfInteg.g);

	vec3 ambient = kd * diffuse + specular;

	return ambient;
}
